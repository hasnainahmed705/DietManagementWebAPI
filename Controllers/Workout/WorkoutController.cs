using DietManagementWebAPI.Models.OtherModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Driver;

[ApiController]
[Route("api/[controller]")]
[AllowAnonymous]
public class WorkoutController : ControllerBase
{
    private readonly MongoDbService _mongoService;

    public WorkoutController(MongoDbService mongoService)
    {
        _mongoService = mongoService;
    }

    [HttpPost]
    [Route("InsertWorkoutSession")]
    public async Task<ActionResult> InsertWorkoutSession(
    [FromBody] WorkoutSessionRequest request)
    {
        using var session = await _mongoService.Client.StartSessionAsync();

        try
        {
            session.StartTransaction();

            var currentDate = DateTime.Parse(request.startTime).Date;

            var filter =
                Builders<WorkoutSessionModel>.Filter.Eq(x => x.userName, request.userName) &
                Builders<WorkoutSessionModel>.Filter.Eq(x => x.sessionName, request.sessionName) &
                Builders<WorkoutSessionModel>.Filter.Eq(x => x.muscleGroupName, request.muscleGroupName) &
                Builders<WorkoutSessionModel>.Filter.Gte(
                    x => x.startTime,
                    currentDate.ToString("yyyy-MM-ddT00:00:00Z")) &
                Builders<WorkoutSessionModel>.Filter.Lt(
                    x => x.startTime,
                    currentDate.AddDays(1).ToString("yyyy-MM-ddT00:00:00Z"));

            var existingSession = await _mongoService.WorkoutSessions
                .Find(session, filter)
                .FirstOrDefaultAsync();

            if (existingSession != null)
            {
                // Only update endTime
                var update = Builders<WorkoutSessionModel>.Update
                    .Set(x => x.endTime, request.endTime);

                await _mongoService.WorkoutSessions.UpdateOneAsync(
                    session,
                    filter,
                    update);

                await session.CommitTransactionAsync();

                return Ok(new
                {
                    message = "Workout session end time updated successfully."
                });
            }

            // Insert new record
            var workoutSession = new WorkoutSessionModel
            {
                userName = request.userName,
                sessionName = request.sessionName,
                muscleGroupName = request.muscleGroupName,
                startTime = request.startTime,
                endTime = request.endTime,
                durationMinutes = request.durationMinutes,
                status = request.status
            };

            await _mongoService.WorkoutSessions.InsertOneAsync(
                session,
                workoutSession);

            await session.CommitTransactionAsync();

            return Ok(new
            {
                message = "Workout session inserted successfully.",
                data = workoutSession
            });
        }
        catch (Exception ex)
        {
            await session.AbortTransactionAsync();

            return StatusCode(500, new
            {
                error = ex.Message
            });
        }
    }

    [HttpPost]
    [Route("InsertWorkoutExercisesLogs")]
    public async Task<ActionResult> InsertWorkoutExercisesLogs(
    [FromBody] List<WorkoutExerciseRequest> requests)
    {
        if (requests == null || !requests.Any())
        {
            return BadRequest(new
            {
                error = "Request body cannot be empty."
            });
        }

        using var session = await _mongoService.Client.StartSessionAsync();

        try
        {
            session.StartTransaction();

            foreach (var request in requests)
            {
                var currentDate = DateTime.Parse(request.date).Date;

                var filter =
                    Builders<WorkoutExerciseModel>.Filter.Eq(x => x.userName, request.userName) &
                    Builders<WorkoutExerciseModel>.Filter.Eq(x => x.exerciseName, request.exerciseName) &
                    Builders<WorkoutExerciseModel>.Filter.Eq(x => x.sessionName, request.sessionName) &
                    Builders<WorkoutExerciseModel>.Filter.Gte(
                        x => x.date,
                        currentDate.ToString("yyyy-MM-ddT00:00:00Z")) &
                    Builders<WorkoutExerciseModel>.Filter.Lt(
                        x => x.date,
                        currentDate.AddDays(1).ToString("yyyy-MM-ddT00:00:00Z"));

                await _mongoService.WorkoutExerciseLogs.DeleteManyAsync(
                    session,
                    filter);
            }

            var workoutExercises = requests.Select(request => new WorkoutExerciseModel
            {
                sessionName = request.sessionName,
                userName = request.userName,
                date = request.date,
                exerciseName = request.exerciseName,
                muscleGroupName = request.muscleGroupName,
                sets = request.sets.Select(set => new WorkoutSetModel
                {
                    setNumber = set.setNumber,
                    reps = set.reps,
                    isCompleted = set.isCompleted
                }).ToList()
            }).ToList();

            await _mongoService.WorkoutExerciseLogs.InsertManyAsync(
                session,
                workoutExercises);

            await session.CommitTransactionAsync();

            return Ok(new
            {
                message = $"{workoutExercises.Count} workout exercise(s) inserted successfully."
            });
        }
        catch (Exception ex)
        {
            await session.AbortTransactionAsync();

            return StatusCode(500, new
            {
                error = ex.Message
            });
        }
    }
}
