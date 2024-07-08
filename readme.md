# ONNX Vision
Web app and service for performing vision processing with ONNX-based vision (multi-modal) models.

[write-up](https://www.linkedin.com/pulse/phi-3-vision-surprisingly-useful-gem-faisal-waris-sxuqc/)

Test with Phi-3 Vision.

## System Requirements
- GPU hardware - specific requirements depend on the vision model used
    - Successfully tested with nVidia 3080 (16GB) and A100 (80GB) GPUs using Phi-3-vision-128k-instruct-onnx-cuda
- nVidia GPU Toolkit version 11.8
    - The GPU Toolkit bin directory must be on path (e.g.: %CUDA_PATH_V11_8%\bin)
    - The current version of Onnx GenAI requires toolkit v11.8 but this may change 
- CUDNN version 8.
    - The bin directory must be on path (e.g. E:\s\cudnn\cudnn-windows-x86_64-8.4.1.50_cuda11.6-archive\bin)
    - Also an Onnx GenAI requirement that may change
- Windows 64 with .Net 8 SDK installed
    - Only tested on windows
    - Should be possible to run on Linux and Mac with minor updates

## Configuration
The application needs two configured values from appSettings.json
- **ModelPath**: Directory that contains the Onnx vision model
- **ModelInstanceCount**: The number of model instances to load in the GPU. The system will be able to handle that many concurrent requests. The instance count depends on the size of the GPU.

## Usage
```
git clone https://github.com/fwaris/OnnxVision
cd OnnxVision
dotnet build 
dotnet run -p ./src/OnnxVision.Server
```
open browser to http://localhost:5045<p>(or whatever is configured in (server root)/Properties/launchSettings.json)

Enter a prompt; select an image and click on Infer. Note first time may be slow as the model is loaded into GPU memory.
Use Ctr-c to exit the application and release the GPU memory.


## Service / Batch usage
- [TestService.fsx](src/OnnxVision.Server/scripts/TestService.fsx) shows how the vision service can be called via http, for a single image.
- [TestBatch.fsx](src/OnnxVision.Server/scripts/TestBatch.fsx) shows how process a batch of images in parallel using throttled calls.
    - In a test run, over 1000 images were processed in less than 5 minutes using 4 model instances on an nVidia A100 GPU. The median image size was 25K bytes.
