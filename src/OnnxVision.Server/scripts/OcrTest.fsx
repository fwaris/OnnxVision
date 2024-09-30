#r "nuget: TesseractOCR, 5.3.5"
//#r "nuget: Tesseract.Drawing"
open TesseractOCR
open TesseractOCR.Enums
open TesseractOCR.Exceptions
open TesseractOCR.Pix
open System
System.Runtime.InteropServices.NativeLibrary.Load(@"C:\Users\fwaris1\.nuget\packages\tesseractocr\5.3.5\x64\tesseract53.dll")
System.Runtime.InteropServices.NativeLibrary.Load(@"C:\Users\fwaris1\.nuget\packages\tesseractocr\5.3.5\x64\leptonica-1.83.1.dll")
let folder = @"C:\Users\fwaris1\OneDrive - T-Mobile USA\Pictures\Screenshots"
let path = @"C:\Users\fwaris1\.nuget\packages\tesseractocr\5.3.5\x64;" + Environment.GetEnvironmentVariable("PATH")
Environment.SetEnvironmentVariable("PATH", path)
let engine = new Engine(@"./tessdata", Language.English, EngineMode.Default)



                {
                    using (var img = Pix.LoadFromFile(testImagePath))
                    {
                        using (var page = engine.Process(img))
                        {
                            var text = page.GetText();
                          