// using UnityEngine;
// using System.Runtime.InteropServices;
// using System.Collections.Generic;

// namespace GPUCompute
// {    
//     public class Buffer<T> : IDisposable
//     {
//         public ComputeBuffer buffer;
//         public string name;
//         public int length, stride;
        
//         public Buffer(int buffer_length, string name) {
//             this.name = name;
//             length = buffer_length;
//             stride = Marshal.SizeOf(typeof(T));

//             T[] initArray = new T[buffer_length];
//             for (int i = 0; i < buffer_length; i++)
//                 initArray[i] = 0;

//             buffer = new ComputeBuffer(buffer_length, stride);
//             buffer.SetData(initArray);
//         }

//         public void Dispose() {
//             if (buffer != null)
//                 buffer.Release();
//             buffer = null;
//         }

//         public void Reset() {
//             Dispose();
//             buffer = new ComputeBuffer(length, stride);
//         }
//     }


//     public class Kernel {
//         ComputeShader Compute;
//         int kernel_id;
//         Vector3Int thread_groups;
//         List<Buffer> buffers;

//         public Kernel(ComputeShader cs, string name, Vector3Int thread_groups) {
//             Compute = cs;
//             kernel_id = Compute.FindKernel(name);
//             this.thread_groups = thread_groups;
//         }

//         public void SetBuffers(List<Buffer<T>> buffers) {
//             this.buffers = buffers;
//         }

//         public void InitBuffers() {
//             foreach (Buffer b in buffers) {
//                 Compute.SetBuffer(kernel_id, b.name, b.buffer);
//             }
//         }

//         public void Run() {
//             Compute.Dispatch(kernel_id, thread_groups.x, thread_groups.y, thread_groups.z);
//         }
//     }
// }