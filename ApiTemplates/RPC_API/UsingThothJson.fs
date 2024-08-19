namespace RpcThothJson

//Compiler directives
#if FABLE_COMPILER
open Thoth.Json
#else
open Thoth.Json.Net

#endif

//Templates -> try-with blocks and Option/Result to be added when used in production

//RPC API created with SATURN and GIRAFFE
//Data format -> JSON
//Client Library -> FsHttp 
//(De)Serialization -> Thoth.Json

module RpcFunctions =  
     
    let add (a : int) (b : int) : int =
        a + b

module RpcApi =

    open System.IO

    open Saturn
    open Giraffe    
    open Microsoft.AspNetCore.Http  

    open ThothCoders

    let private rpcHandler : HttpHandler = //GIRAFFE

        fun (next : HttpFunc) (ctx : HttpContext) //GIRAFFE
            ->
             task 
                 {     
                     use reader = new StreamReader(ctx.Request.Body)
                     let! body = reader.ReadToEndAsync()
                                                             
                     let payload = 
                        Decode.fromString decoder body  
                        |> function
                            | Ok value -> value
                            | Error _  -> { a = 0; b = 0 } //To replace with default values in production                                         
                                
                     let response =                         
                         let a = payload.a
                         let b = payload.b 
                         
                         { 
                             Result = RpcFunctions.add a b 
                         }

                     let responseJson = Encode.toString 2 (encoder response) //2 = the number of spaces used for indentation in the JSON structure                   
                
                     ctx.Response.ContentType <- "application/json"
                     return! text responseJson next ctx //GIRAFFE
                 }
                    
    let private apiRouter =  //SATURN
        router
            {
                post "/" rpcHandler
            }

    let private app =  //SATURN
        application 
            {
                use_router apiRouter
                url "http://0.0.0.0:8080"
                memory_cache
                use_static "static"
                use_gzip
            }

    let internal runRpcHandlerThothJson () =  //SATURN
        run app

  

