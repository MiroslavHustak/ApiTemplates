namespace RpcSystemTextJson

//Templates -> try-with blocks and Option/Result to be added when used in production

//RPC API created with SATURN and GIRAFFE
//Data format -> JSON
//Client Library -> FsHttp 
//(De)Serialization -> System.Text.Json

module RpcFunctions =  
     
    let add = (+) 

module RpcApi =

    open System.IO

    open Saturn
    open Giraffe
    open System.Text.Json    
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

    let private rpcHandler : HttpHandler =  //GIRAFFE

        fun (next : HttpFunc) (ctx : HttpContext)  //GIRAFFE
            ->
             task 
                 {                    
                     use reader = new StreamReader(ctx.Request.Body)
                     let! body = reader.ReadToEndAsync()
                
                     let options = JsonSerializerOptions(PropertyNameCaseInsensitive = true)
                     let payload = JsonSerializer.Deserialize<Parameters>(body, options)
                                
                     let response =                         
                         let a = payload.a
                         let b = payload.b 
                         
                         { 
                             Result = RpcFunctions.add a b 
                         }
                
                     let responseJson = JsonSerializer.Serialize(response)
                
                     ctx.Response.ContentType <- "application/json"
                     return! text responseJson next ctx  //GIRAFFE
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

    let internal runRpcHandlerTextJson () =  //SATURN
        run app

  