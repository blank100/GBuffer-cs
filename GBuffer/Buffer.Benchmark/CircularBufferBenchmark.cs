using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using Gal.Core;

// CircularBuffer<T> vs Queue<T> 的基准
// 覆盖：Enqueue/Dequeue 主循环、满容时 TryEnqueue(overwrite)、TryDequeue 空容场景

[Config(typeof(BenchmarkConfig))]
public class CircularBufferBenchmark {
    private const int Capacity = 1024;

    [Params(1, 16, 128, 1024)] public int N;

    // ------------------
    // Enqueue + Dequeue 主循环
    // ------------------

    [Benchmark(Baseline = true)]
    [BenchmarkCategory("EnqueueDequeue")]
    public int Queue_EnqueueDequeue() {
        var queue = new Queue<int>(Capacity);
        int result = 0;
        for (int i = 0; i < N; i++) queue.Enqueue(i);
        for (int i = 0; i < N; i++) result ^= queue.Dequeue();
        return result;
    }

    [Benchmark]
    [BenchmarkCategory("EnqueueDequeue")]
    public int Circular_EnqueueDequeue() {
        var buffer = new CircularBuffer<int>(Capacity);
        int result = 0;
        for (int i = 0; i < N; i++) buffer.Enqueue(i);
        for (int i = 0; i < N; i++) result ^= buffer.Dequeue();
        return result;
    }

    // ------------------
    // 满容 overwrite：TryEnqueue(overwrite: true)
    // ------------------

    [Benchmark]
    [BenchmarkCategory("Overwrite")]
    public int Queue_OverwriteSim() {
        var queue = new Queue<int>(Capacity);
        for (int i = 0; i < Capacity; i++) queue.Enqueue(i);
        int result = 0;
        for (int i = 0; i < N; i++) {
            if (queue.Count >= Capacity) result ^= queue.Dequeue();
            queue.Enqueue(i);
        }
        return result;
    }

    [Benchmark]
    [BenchmarkCategory("Overwrite")]
    public int Circular_TryEnqueueOverwrite() {
        var buffer = new CircularBuffer<int>(Capacity);
        for (int i = 0; i < Capacity; i++) buffer.Enqueue(i);
        int result = 0;
        for (int i = 0; i < N; i++) {
            if (buffer.TryEnqueue(i, overwrite: true)) result ^= i;
        }
        return result;
    }

    // ------------------
    // TryDequeue 空容场景
    // ------------------

    [Benchmark]
    [BenchmarkCategory("TryDequeue")]
    public int Circular_TryDequeue() {
        var buffer = new CircularBuffer<int>(Capacity);
        int result = 0;
        for (int i = 0; i < N; i++) {
            buffer.Enqueue(i);
            if (buffer.TryDequeue(out int item)) result ^= item;
            if (buffer.TryDequeue(out int empty)) result ^= empty; // 第二次必空
        }
        return result;
    }

    [Benchmark]
    [BenchmarkCategory("TryDequeue")]
    public int Queue_TryDequeue() {
        var queue = new Queue<int>(Capacity);
        int result = 0;
        for (int i = 0; i < N; i++) {
            queue.Enqueue(i);
            if (queue.Count > 0) result ^= queue.Dequeue();
            if (queue.Count > 0) result ^= queue.Dequeue(); // 第二次必空
        }
        return result;
    }
}
