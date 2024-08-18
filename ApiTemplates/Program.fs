namespace LLM_API

open System
open FsHttp

open RpcThothJson.RpcApi
open RpcSystemTextJson.RpcApi
open RpcNewtonsoftJson.RpcApi

open RestApiThothJson.ThothJson
open RestApiNewtonsoftJson.NewtonsoftJson
open RestApiSystemTextJson.RestApiTextJson

module API = 

    //runApiTextJson ()
    //runApiNewtonsoftJson ()
    runApiThothJson ()

    //runRpcHandlerTextJson ()
    //runRpcHandlerNewtonsoftJson ()
    //runRpcHandlerThothJson ()
    

    Console.ReadKey() |> ignore