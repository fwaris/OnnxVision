#r "nuget: LLamaSharp"
#r "nuget: LLamaSharp.Backend.Cpu"
#r "nuget: FSharp.Control.AsyncSeq"
open System
open System.Text
open System.IO
open LLama
open LLama.Common
open LLama.Sampling
open LLama.Native
open FSharp.Control
let modelFile = @"C:\s\models\gemma-2-2b-GGUF\gemma-2-2b-it-Q8_0.gguf"

let parms = ModelParams(modelFile)
let model = LLamaWeights.LoadFromFile(parms)

let exec = new Batched.BatchedExecutor(model,parms)

let conv = exec.Create()

let prompt q = """
<start_of_turn>user
{q}
<start_of_turn>model
"""

