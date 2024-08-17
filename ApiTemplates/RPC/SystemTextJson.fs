namespace RpcSystemTextJson

module RpcFunctions =  
     
    let add (a : int) (b : int) : int =
        a + b

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
            result : int
        }

    let private rpcHandler : HttpHandler =

        fun (next : HttpFunc) (ctx : HttpContext)
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
                             result = RpcFunctions.add a b 
                         }
                
                     let responseJson = JsonSerializer.Serialize(response)
                
                     ctx.Response.ContentType <- "application/json"
                     return! text responseJson next ctx
                 }
                    
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

    let internal runRpcHandlerTextJson () =
        run app

  