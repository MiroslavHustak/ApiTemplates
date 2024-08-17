namespace LLM_API

open System
open FsHttp

open RpcSystemTextJson.RpcApi
open RpcNewtonsoftJson.RpcApi
open RestApiNewtonsoftJson.NewtonsoftJson
open RestApiSystemTextJson.RestApiTextJson

module API = 

    //runApiTextJson ()
    //runApiNewtonsoftJson ()
    //runRpcHandlerTextJson ()
    runRpcHandlerNewtonsoftJson ()

    Console.ReadKey() |> ignore