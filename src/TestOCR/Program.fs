module TestOCR
open System
open System.IO
open TesseractOCR
open TesseractOCR.Enums
open TesseractOCR.Exceptions
open TesseractOCR.Pix

let trainData = @"C:\s\legal"

let processImage (imagePath:string) =
    use engine = new Engine(trainData, Language.English, EngineMode.Default)
    use img = Pix.Image.LoadFromFile(imagePath)
    use page = engine.Process(img)
    printf $"{imagePath}, confidence: {page.MeanConfidence}\n"
    let text = page.Text
    let pout = imagePath + ".0_1.txt"
    File.WriteAllText(pout,text)

let pdfToImage2 (pdf:string) = 
    Image.Conversion.exportImagesToDisk (Some(120uy,120uy,120uy)) pdf

let folder = @"C:\s\legal\docs"

Directory.GetFiles(folder,"*.pdf") |> Seq.iter pdfToImage2


let files = Directory.GetFiles(folder,"*.jpeg")
for file in files do
    processImage file
