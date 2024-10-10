namespace LerUI
open System
open System.IO
open System.Reflection

module Env =
    let DEF_MODEL_PATH = @"C:\s\models\Phi-3.5-mini-instruct-onnx\cpu_and_mobile\cpu-int4-awq-block-128-acc-level-4"
    let LOCAL_MODEL_PATH = @"data\Phi-3.5-mini-instruct-onnx\cpu_and_mobile\cpu-int4-awq-block-128-acc-level-4"
    let DEF_TRAIN_DATA_PATH =  @"C:\s\legal"
    let LOCAL_TRAIN_DATA_PATH = @"data\trainData"

    let asmDir = lazy(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location))

    let modelPath() =
        let path = Path.Combine(asmDir.Value, LOCAL_MODEL_PATH)
        //failwith path
        if Directory.Exists path then
            path
        else
            DEF_MODEL_PATH

    let trainDataPath() =
        let path = Path.Combine(asmDir.Value, LOCAL_TRAIN_DATA_PATH)
        if Directory.Exists path then
            path
        else
            DEF_TRAIN_DATA_PATH