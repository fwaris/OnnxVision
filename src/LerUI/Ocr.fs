namespace LerUI
open System
open System
open System.IO
open TesseractOCR
open TesseractOCR.Enums
open TesseractOCR.Exceptions
open TesseractOCR.Pix

module OCR =
    let trainData = @"C:\s\legal"

    let processImage (imagePath:string) appendLog =
        use engine = new Engine(trainData, Language.English, EngineMode.Default)
        use img = Pix.Image.LoadFromFile(imagePath)
        use page = engine.Process(img)
        appendLog $"{Path.GetFileName(imagePath)}, confidence: {page.MeanConfidence}"
        let text = page.Text
        let pout = imagePath + ".0_1.txt"
        File.WriteAllText(pout,text)

