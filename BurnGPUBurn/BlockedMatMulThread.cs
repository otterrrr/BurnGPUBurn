/*

Copyright (c) 2020 Taesik Yoon

Permission is hereby granted, free of charge, to any person
obtaining a copy of this software and associated documentation
files (the "Software"), to deal in the Software without
restriction, including without limitation the rights to use,
copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the
Software is furnished to do so, subject to the following
conditions:

The above copyright notice and this permission notice shall be
included in all copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES
OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT
HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY,
WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING
FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR
OTHER DEALINGS IN THE SOFTWARE.

*/

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Cloo;
using Cloo.Bindings;

namespace BurnGPUBurn
{
    internal class BlockedMatMulThread
    {
        private const string clProgramSource =
@"
#define blksz 16
__kernel void mmul(
                const unsigned int             N,
                __global const float* restrict A,
                __global const float* restrict B,
                __global       float* restrict C,
                __local        float* restrict Awrk,
                __local        float* restrict Bwrk)
{
    int kloc, Kblk;
    float Ctmp=0.0f;

    //  This work-item will compute element C(i,j)
    const int i = get_global_id(0);
    const int j = get_global_id(1);

    // Element C(i,j) is in block C(Iblk,Jblk)
    const int Iblk = get_group_id(0);
    const int Jblk = get_group_id(1);

    // C(i,j) is element C(iloc, jloc) of block C(Iblk, Jblk)
    const int iloc = get_local_id(0);
    const int jloc = get_local_id(1);

    // The number of blocks are the same in each dimension
    const int Num_BLK = N/blksz;

    // Setup the upper-left-corner (base address) for the A and
    // B blocks plus the increments to advance base addresses as
    // we loop over blocks
          int Abase = Jblk*N*blksz;    
    const int Ainc  = blksz;

          int Bbase = Iblk*blksz;
    const int Binc  = blksz*N;


    // C(Iblk,Jblk) = (sum over Kblk) A(Iblk,Kblk)*B(Kblk,Jblk)
    for (Kblk = 0;  Kblk<Num_BLK;  Kblk++)
    {
       // Load A(Iblk,Kblk) and B(Kblk,Jblk) into local memory.
       // Each work-item loads a single element of the two blocks
       // which are shared with the entire work-group.

       Awrk[jloc*blksz+iloc] = A[Abase+jloc*N+iloc];
       Bwrk[jloc*blksz+iloc] = B[Bbase+jloc*N+iloc];

       barrier(CLK_LOCAL_MEM_FENCE);

       // Compute dot products over local blocks to find
       // the contribution to C(i,j) from this block
       #pragma unroll
       for (kloc=0; kloc<blksz; kloc++)
          Ctmp += Awrk[jloc*blksz+kloc] * Bwrk[kloc*blksz+iloc];

       barrier(CLK_LOCAL_MEM_FENCE);
       Abase += Ainc;
       Bbase += Binc;
    }
 
    // update global C matrix 
    C[j*N+i] = Ctmp;
}
";

        public BlockedMatMulThread(ComputePlatform platform, ComputeDevice device, int inN, float[] inA, float[] inB)
        {
            ComputeDevice[] devices = { device };
            context = new ComputeContext(devices, new ComputeContextPropertyList(platform), null, IntPtr.Zero);
            program = new ComputeProgram(context, clProgramSource);

            try
            {
                program.Build(null, null, null, IntPtr.Zero);
            }
            catch (BuildProgramFailureComputeException e)
            {
                string buildLog = program.GetBuildLog(device);
                Console.WriteLine(buildLog);
                throw;
            }
            kernel = program.CreateKernel("mmul");
            queue = new ComputeCommandQueue(context, device, ComputeCommandQueueFlags.None);

            n = inN;
            a = new ComputeBuffer<float>(context, ComputeMemoryFlags.ReadOnly | ComputeMemoryFlags.CopyHostPointer, inA);
            b = new ComputeBuffer<float>(context, ComputeMemoryFlags.ReadOnly | ComputeMemoryFlags.CopyHostPointer, inB);
            int resultCount = (int)(device.MaxMemoryAllocationSize / n / n / sizeof(float));
            results = new ComputeBuffer<float>[resultCount];
            for (int i = 0; i < results.Length; ++i)
                results[i] = new ComputeBuffer<float>(context, ComputeMemoryFlags.WriteOnly, n*n);
            
            HaltRequested = false;
            operationCounter = 0;
            completionCounter = 0;
            startTime = 0;
        }

        ~BlockedMatMulThread()
        {
            Dispose();
        }

        public void Dispose()
        {
            queue.Dispose();
            kernel.Dispose();
            program.Dispose();
            foreach (var result in results)
                result.Dispose();
            b.Dispose();
            a.Dispose();
            context.Dispose();
        }

        public void Run()
        {
            startTime = Stopwatch.GetTimestamp();

            const int blockSize = 16;
            ulong _n = (ulong)n;
            HaltRequested = false;
            operationCounter = 0;
            completionCounter = 0;
            
            for (int i = 0; true ; i = (i+1)%results.Length)
            {
                if (HaltRequested)
                    break;
                kernel.SetValueArgument(0, (uint)n);
                kernel.SetMemoryArgument(1, a);
                kernel.SetMemoryArgument(2, b);
                kernel.SetMemoryArgument(3, results[i]);
                kernel.SetLocalArgument(4, sizeof(float) * blockSize * blockSize);
                kernel.SetLocalArgument(5, sizeof(float) * blockSize * blockSize);
                queue.Execute(kernel, null, new long[] {n,n}, new long[] {blockSize,blockSize}, null);
                queue.Finish();

                operationCounter += (2 * _n * _n * _n);
                completionCounter++;

                if (HaltRequested)
                    break;
            }
            operationCounter = 0;
            completionCounter = 0;
        }

        private readonly int n;
        private readonly ComputeBuffer<float> a;
        private readonly ComputeBuffer<float> b;
        private readonly ComputeBuffer<float>[] results;
        private readonly ComputeContext context;
        private readonly ComputeProgram program;
        private readonly ComputeKernel kernel;
        private readonly ComputeCommandQueue queue;
        private ulong operationCounter;
        private ulong completionCounter;
        private long startTime;

        public float MFLOPS
        {
            get
            {
                ulong elapsedTicks = (ulong)(Stopwatch.GetTimestamp() - startTime);
                ulong frequency = (ulong)Stopwatch.Frequency;
                return completionCounter > 0 ? Convert.ToSingle(operationCounter / 1000000ul * frequency /elapsedTicks) : 0;
            }
        }
        public bool HaltRequested { get; set; }
    }
}
