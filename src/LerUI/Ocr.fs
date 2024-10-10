namespace LerUI
open System
open System
open System.IO
open TesseractOCR
open TesseractOCR.Enums
open TesseractOCR.Exceptions
open TesseractOCR.Pix

module OCR =

    let processImage (i:int) (imagePath:string) appendLog =
        async {
            use engine = new Engine(Env.trainDataPath(), Language.English, EngineMode.Default)
            use img = Pix.Image.LoadFromFile(imagePath)
            use page = engine.Process(img)
            appendLog $"img-to-text page {i}, confidence: {page.MeanConfidence}"
            let text = page.Text
            let pout = imagePath + ".0_1.txt"
            File.WriteAllText(pout,text)
        }

