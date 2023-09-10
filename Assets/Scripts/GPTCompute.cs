using UnityEngine;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace GPTCompute
{
    public interface IBuffer
    {
        ComputeBuffer buffer { get; }
        string Name { get; }
    }

    public class Buffer<T> : IBuffer, IDisposable where T : struct
    {
        public ComputeBuffer buffer { get; private set; }
        public string Name { get; private set; }
        public int Length { get; private set; }
        public int Stride { get; private set; }

        public Buffer(int length, string name)
        {
            Name = name;
            Length = length;
            Stride = Marshal.SizeOf(typeof(T));

            T[] initArray = new T[length];
            for (int i = 0; i < length; i++)
                initArray[i] = default(T);

            try {
                buffer = new ComputeBuffer(length, Stride);
                buffer.SetData(initArray);
            }
            catch (Exception ex) {
                Debug.LogError($"Failed to create ComputeBuffer: {ex.Message}");
                buffer = null;
            }
        }

        public Buffer(int length, string name, T[] initArray)
        {
            Name = name;
            Length = length;
            Stride = Marshal.SizeOf(typeof(T));

            try {
                buffer = new ComputeBuffer(length, Stride);
                buffer.SetData(initArray);
            }
            catch (Exception ex) {
                Debug.LogError($"Failed to create ComputeBuffer: {ex.Message}");
                buffer = null;
            }
        }

        public Buffer(int length, string name, T[] initArray, ComputeBufferType bufferType)
        {
            Name = name;
            Length = length;
            Stride = Marshal.SizeOf(typeof(T));

            try {
                buffer = new ComputeBuffer(length, Stride, bufferType);
                buffer.SetData(initArray);
            }
            catch (Exception ex) {
                Debug.LogError($"Failed to create ComputeBuffer: {ex.Message}");
                buffer = null;
            }
        }

        public void Dispose() {
            if (buffer != null) {
                buffer.Release();
                buffer = null;
            }
        }

        public void Reset() {
            Dispose();
            buffer = new ComputeBuffer(Length, Stride);
        }

        public void Reset(int length) {
            Dispose();
            buffer = new ComputeBuffer(length, Stride);
        }
    }

    public class Kernel
    {
        ComputeShader Compute;
        int kernel_id;
        Vector3Int thread_groups;
        List<IBuffer> buffers;

        public Kernel(ComputeShader cs, string name, Vector3Int threadGroups)
        {
            Compute = cs;
            kernel_id = Compute.FindKernel(name);
            thread_groups = threadGroups;
            buffers = new List<IBuffer>();
        }

        public void SetBuffers(List<IBuffer> buffers)
        {
            this.buffers = buffers;
        }

        public void AddBuffer(IBuffer buffer)
        {
            buffers.Add(buffer);
        }

        public void InitBuffers()
        {
            foreach (IBuffer buffer in buffers)
            {
                Compute.SetBuffer(kernel_id, buffer.Name, buffer.buffer);
            }
        }

        public void Run()
        {
            Compute.Dispatch(kernel_id, thread_groups.x, thread_groups.y, thread_groups.z);
        }
    }
}