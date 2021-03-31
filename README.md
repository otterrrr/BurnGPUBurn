# BurnGPUBurn

A stress test program over multiple GPUs of cross-vendor

### Motivation

[gpu-burn](https://github.com/wilicc/gpu-burn) is the existing stress test program to see MFLOPS, temperature and power-consumption but this is working only on NVIDIA GPUs. In this manner, BurnGPUBurn looks an extention of gpu-burn considering cross-vendor GPUs like a combination of AMD+NVIDIA or Intel+AMD

### Open source dependency

* [Open Hardware Monitor](https://github.com/openhardwaremonitor)
* [Cloo](https://github.com/clSharp/Cloo)

### Screenshot

![burngpuburn](https://user-images.githubusercontent.com/1368591/100224389-1de75d80-2f60-11eb-93c6-26e696f5cda0.png)

Enable checkbox per device to perform stress test

### Disclaimer

**<span style="color:red">This software isn't responsible for any damage. Above all, please make sure your Power Supply Unit can supply sufficient power to your system</span>**

### Future items maybe...

* Correct mapping between OpenCL and ADL/NVAPI devices
* Test if it's working on Linux with mono
