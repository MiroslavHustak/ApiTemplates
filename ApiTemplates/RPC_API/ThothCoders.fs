namespace RpcThothJson

//Compiler-independent template suitable for Shared as well

//************************************************

//Compiler directives
#if FABLE_COMPILER
open Thoth.Json
#else
open Thoth.Json.Net
#endif

//*********************************************** 

type Parameters =
    {
        a : int
        b : int 
    } 

type Results =
    {
        Result : int
    }

module ThothCoders =   

    let internal encoder (result : Results) =
        Encode.object
            [
                "Result", Encode.int result.Result
            ]

    let internal decoder : Decoder<Parameters> =
        Decode.object
            (fun get ->
                      {
                          a = get.Required.Field "a" Decode.int
                          b = get.Required.Field "b" Decode.int
                      }
            )