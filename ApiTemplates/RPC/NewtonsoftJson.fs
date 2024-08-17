namespace RpcNewtonsoftJson

module RpcFunctions =  
     
    let add (a : int) (b : int) : int =
        a + b

module RpcApi =

    open System.IO
    
    open Saturn
    open Giraffe
    open Newtonsoft.Json  
    open Microsoft.AspNetCore.Http 

    type Parameters =
        {
            a : int
            b : int 
        } 
        
    type Results =
        {
            result : int
        }

    let private rpcHandler : HttpHandler =

        fun (next : HttpFunc) (ctx : HttpContext)
            ->
             async 
                 {    
                     use reader = new StreamReader(ctx.Request.Body)
                     let! body = reader.ReadToEndAsync() |> Async.AwaitTask
            
                     let payload = JsonConvert.DeserializeObject<Parameters>(body)
                                                                    
                     let response =                         
                         let a = payload.a
                         let b = payload.b 
                         
                         { 
                             result = RpcFunctions.add a b 
                         }
                
                     let responseText = JsonConvert.SerializeObject(response)
                     ctx.Response.ContentType <- "application/json" 

                     return! text responseText next ctx |> Async.AwaitTask
                 }
             |> Async.StartImmediateAsTask  
                    
    let private apiRouter =
        router
            {
                post "/" rpcHandler
            }

    let private app =
        application 
            {
                use_router apiRouter
                url "http://0.0.0.0:8080"
                memory_cache
                use_static "static"
                use_gzip
            }

    let internal runRpcHandlerNewtonsoftJson () =
        run app

  