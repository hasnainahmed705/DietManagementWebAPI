using DietManagementWebAPI.Models.DBModels;
using DietManagementWebAPI.Models.EmailModels;
using DietManagementWebAPI.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Driver;
using Org.BouncyCastle.Asn1.Ocsp;

namespace DietManagementWebAPI.Controllers.EmailSending
{
    [ApiController]
    [Route("api/[controller]")]
    public class EmailController : ControllerBase
    {
        private readonly EmailService _emailService;
        private readonly MongoDbService _mongoService;

        public EmailController(EmailService emailService, MongoDbService mongoService)
        {
            _emailService = emailService;
            _mongoService = mongoService;
        }

        [AllowAnonymous]
        [HttpPost("SendEmail")]
        public async Task<IActionResult> SendOTPEmail(string email)
        {
            try
            {
                var otp = Random.Shared.Next(100000, 999999).ToString();
                string createdDateNow = DateTime.UtcNow.ToString();
                string expiryTimeNow = DateTime.UtcNow.AddSeconds(50).ToString();

                var existingUser = await _mongoService.Users
                                .Find(u => u.email == email)
                                .FirstOrDefaultAsync();

                //var existingOtpBeforeExpiry=await _mongoService.UserOtps.Find(u => u.email == email && u.userName== existingUser.userName)
                //    .FirstOrDefaultAsync();

                var newOtp = new UserOtpsModel
                {
                    userName = existingUser.userName,
                    email = email,
                    otp = otp,
                    createdAt = createdDateNow,
                    expiresAt = expiryTimeNow,
                    isVerified = false,
                };

                await _emailService.SendOtpEmailAsync(
                    email,
                    otp
                );

                await _mongoService.UserOtps.InsertOneAsync(newOtp);


                return Ok(new
                {
                    success = true,
                    message = "OTP has been sent successfully. Please check your email inbox."
                });

            }
            catch (Exception ex)
            {
                return BadRequest(new
                {
                    success = false,
                    message = ex.Message,
                });
            }
        }

        [AllowAnonymous]
        [HttpPost("CheckEmailVerification")]
        public async Task<IActionResult> CheckEmailVerification(string otp, string email)
        {
            var existingOtp = await _mongoService.UserOtps
                                .Find(u => u.email == email && u.otp == otp && u.isVerified==false)
                                .FirstOrDefaultAsync();

            if (existingOtp != null)
            {
                DateTime expiryTime = DateTime.Parse(existingOtp.expiresAt);

                if (expiryTime > DateTime.UtcNow)
                {
                    using var session = await _mongoService.Client.StartSessionAsync();

                    try
                    {
                        session.StartTransaction();

                        var otpFilter = Builders<UserOtpsModel>.Filter.Eq(u => u.email, email)
                            & Builders<UserOtpsModel>.Filter.Eq(u => u.otp, otp)
                            & Builders<UserOtpsModel>.Filter.Eq(u => u.isVerified, false);

                        var otpUpdate = Builders<UserOtpsModel>.Update.Set(u => u.isVerified, true);

                        var updatedUser = await _mongoService.UserOtps.FindOneAndUpdateAsync(
                            session,
                            otpFilter,
                            otpUpdate,
                            new FindOneAndUpdateOptions<UserOtpsModel, UserOtpsModel>
                            {
                                ReturnDocument = ReturnDocument.After
                            });

                        await session.CommitTransactionAsync();

                        return Ok(new
                        {
                            updatedUser.userName,
                            updatedUser.email,
                            updatedUser.isVerified
                        });
                    }
                    catch (Exception ex)
                    {
                        await session.AbortTransactionAsync();
                        return StatusCode(500, new
                        {
                            message = "Update failed, all changes rolled back",
                            error = ex.Message
                        });
                    }
                }
                else
                {
                    return BadRequest(new
                    {
                        success = false,
                        message = "Your OTP has expired. Please request a new OTP.",
                    });
                }
                
            }
            else
            {
                return BadRequest(new
                {
                    success = false,
                    message = "Invalid OTP. Please enter the correct OTP.",
                });
            }

        }
    }
}