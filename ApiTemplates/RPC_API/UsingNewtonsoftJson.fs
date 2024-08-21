namespace RpcNewtonsoftJson

//Templates -> try-with blocks and Option/Result to be added when used in production

//RPC API created with SATURN and GIRAFFE
//Data format -> JSON
//Client Library -> FsHttp 
//(De)Serialization -> Newtonsoft.Json

module RpcFunctions =  
     
    let add = (+) 

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
            Result : int
        }

    let private rpcHandler : HttpHandler =   //GIRAFFE

        fun (next : HttpFunc) (ctx : HttpContext)   //GIRAFFE
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
                             Result = RpcFunctions.add a b 
                         }
                
                     let responseText = JsonConvert.SerializeObject(response)
                     ctx.Response.ContentType <- "application/json" 

                     return! text responseText next ctx |> Async.AwaitTask   //GIRAFFE
                 }
             |> Async.StartImmediateAsTask  
                    
    let private apiRouter =   //SATURN
        router
            {
                post "/" rpcHandler
            }

    let private app =   //SATURN
        application 
            {
                use_router apiRouter
                url "http://0.0.0.0:8080"
                memory_cache
                use_static "static"
                use_gzip
            }

    let internal runRpcHandlerNewtonsoftJson () =   //SATURN
        run app

  