using System.Runtime.CompilerServices;
using System.Threading.Channels;
using GlassBridge;
using LandRunner.Services;

namespace LandRunner.Test;

public class HeadTrackingServiceTests
{
    [Fact]
    public async Task TrackingUpdatesClampOffsetsToUnitRange()
    {
        var device = new FakeImuDevice();
        using var service = new HeadTrackingService();

        var tcs = new TaskCompletionSource<TrackingData>(TaskCreationOptions.RunContinuationsAsynchronously);
        var updates = 0;
        service.TrackingUpdated += (_, data) =>
        {
            if (Interlocked.Increment(ref updates) == 2)
            {
                tcs.TrySetResult(data);
            }
        };

        service.StartTracking(device);

        device.Enqueue(CreateImuData(Quaternion.Identity));
        device.Enqueue(CreateImuData(CreateYawQuaternion(90f)));

        var tracking = await WaitWithTimeoutAsync(tcs.Task);

        Assert.InRange(tracking.HorizontalOffset, -1f, 1f);
        Assert.Equal(-1f, tracking.HorizontalOffset, 3);
        Assert.Equal(0f, tracking.VerticalOffset, 3);
        Assert.Equal(0f, tracking.RotationAngle, 3);

        await service.StopTrackingAsync();
        await device.DisposeAsync();
    }

    private static ImuData CreateImuData(Quaternion quaternion)
        => new()
        {
            Quaternion = quaternion,
            EulerAngles = new EulerAngles(0, 0, 0),
            Timestamp = 0,
            MessageCounter = 0
        };

    private static Quaternion CreateYawQuaternion(float degrees)
    {
        var radians = degrees * MathF.PI / 180f;
        var half = radians / 2f;
        return new Quaternion(MathF.Cos(half), 0f, 0f, MathF.Sin(half));
    }

    private static async Task<T> WaitWithTimeoutAsync<T>(Task<T> task)
    {
        var completed = await Task.WhenAny(task, Task.Delay(TimeSpan.FromSeconds(2)));
        if (completed != task)
        {
            throw new TimeoutException("Timed out waiting for tracking update.");
        }

        return await task;
    }

    private sealed class FakeImuDevice : IImuDevice
    {
        private readonly Channel<ImuData> _channel = Channel.CreateUnbounded<ImuData>();

        public bool IsConnected => true;

        public void Enqueue(ImuData data) => _channel.Writer.TryWrite(data);

        public async IAsyncEnumerable<ImuData> GetImuDataStreamAsync(
            [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            while (await _channel.Reader.WaitToReadAsync(cancellationToken))
            {
                while (_channel.Reader.TryRead(out var data))
                {
                    yield return data;
                }
            }
        }

        public ValueTask DisposeAsync()
        {
            _channel.Writer.TryComplete();
            return ValueTask.CompletedTask;
        }
    }
}
