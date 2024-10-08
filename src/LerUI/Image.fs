namespace LerUI

module Image =
    let imagePath docPath pageNum imageNum = docPath + $".{pageNum}_{imageNum}.jpeg"
    let imageTextPath imagePath = imagePath + ".txt"


[<RequireQualifiedAccess>]
module Conversion =
    open System.Drawing
    open System.Drawing.Imaging

    let addBytes (bitmap:Bitmap) (bytes:byte[]) =
        let rect = new Rectangle(0,0,bitmap.Width,bitmap.Height)
        let bmpData = bitmap.LockBits(rect,ImageLockMode.ReadWrite,bitmap.PixelFormat)
        let ptr = bmpData.Scan0
        let bytesCount = bytes.Length
        System.Runtime.InteropServices.Marshal.Copy(bytes,0,ptr,bytesCount)
        bitmap.UnlockBits(bmpData)

    let jpegEncoder (mimeType:string) =
        ImageCodecInfo.GetImageEncoders()
        |> Array.tryFind (fun codec -> codec.MimeType = mimeType)

    let saveBmp (bmp:Bitmap) (outPath:string) =
        let qualityEncoder = Encoder.Quality;
        use qualityParameter = new EncoderParameter(qualityEncoder, 90);
        use encoderParms = new EncoderParameters(1)
        encoderParms.Param.[0] <- qualityParameter
        let codec = jpegEncoder "image/jpeg" |> Option.defaultWith (fun _ -> failwith "jpeg codec not found")
        bmp.Save(outPath,codec,encoderParms)

    ///Export entire pages as jpeg images to disk
    ///Image file paths are <input path>_{page#}_0.jpeg
    let exportImagesToDisk (backgroundRGB:(byte*byte*byte) option) (path:string) =
        use inst = Docnet.Core.DocLib.Instance
        use reader = inst.GetDocReader(path,Docnet.Core.Models.PageDimensions(1.0))
        [0 .. reader.GetPageCount()-1]
        |> List.iter (fun i ->
            use page = reader.GetPageReader(i)
            let imgBytes =
                match backgroundRGB with
                | Some (red,green,blue) ->  page.GetImage(new Docnet.Core.Converters.NaiveTransparencyRemover(red,blue,green))
                | None                  ->  page.GetImage()
            let w,h = page.GetPageWidth(),page.GetPageHeight()
            use bmp = new Bitmap(w,h,PixelFormat.Format32bppArgb)
            addBytes bmp imgBytes
            let outPath = Image.imagePath path i 0
            saveBmp bmp outPath)

    ///Export entire pages as jpeg images to disk
    ///Image file paths are <input path>_{page#}_0.jpeg
    let exportImagesToDiskScaled (backgroundRGB:(byte*byte*byte) option) (scale:float) (path:string) appendLog =
        use inst = Docnet.Core.DocLib.Instance
        use reader = inst.GetDocReader(path,Docnet.Core.Models.PageDimensions(scale))
        [0 .. reader.GetPageCount()-1]
        |> List.iter (fun i ->
            appendLog $"Exporting page {i} as image"
            use page = reader.GetPageReader(i)
            let imgBytes =
                match backgroundRGB with
                | Some (red,green,blue) ->  page.GetImage(new Docnet.Core.Converters.NaiveTransparencyRemover(red,blue,green))
                | None                  ->  page.GetImage()
            let w,h = page.GetPageWidth(),page.GetPageHeight()
            use bmp = new Bitmap(w,h,PixelFormat.Format32bppArgb)
            addBytes bmp imgBytes
            let outPath = Image.imagePath path i 0
            saveBmp bmp outPath)

