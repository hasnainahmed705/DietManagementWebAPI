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
                message = $"{workoutExercises.Count} workout exercise(s) inserted successfully.",
                insertedCount = workoutExercises.Count,
                data = workoutExercises
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
