namespace RestApiThothJson

//Compiler directives
#if FABLE_COMPILER
open Thoth.Json
#else
open Thoth.Json.Net

#endif

//Templates -> try-with blocks and Option/Result to be added when used in production

//REST API created with SATURN and GIRAFFE
//Data format -> JSON
//Client Library -> FsHttp 
//(De)Serialization -> Thoth.Json

[<Struct>]
type internal PyramidOfDoom = PyramidOfDoom with    
    member _.Bind((optionExpr, err), nextFunc) =
        match optionExpr with
        | Some value -> nextFunc value 
        | _          -> err  
    member _.Return x : 'a = x   
    member _.ReturnFrom x : 'a = x 
    member _.TryFinally(body, compensation) =
        try body()
        finally compensation()
    member _.Zero () = ()
    member _.Using(resource, binder) =
        use r = resource
        binder r

module ThothJson =

    open System
    open System.IO
    open System.Data
    
    open Saturn
    open Giraffe
    open Microsoft.AspNetCore.Http    

    open ThothCoders
    
    // ************** GET *******************
           
    // curl -X GET http://localhost:8080/    
    let private getHandler : HttpHandler =

        fun (next : HttpFunc) (ctx : HttpContext)
            ->
             let response = 
                 {
                     Message = "Hello, World!"
                     Timestamp = System.DateTime.UtcNow.ToString("o") // ISO 8601 format
                 }

             let responseJson = Encode.toString 2 (encoderGet response) //2 = the number of spaces used for indentation in the JSON structure  
                        
             ctx.Response.ContentType <- "application/json"
             text responseJson next ctx
    
    
    // ************** POST *******************    

    // Handlers for POST request
    // curl -X POST http://localhost:8080/ -H "Content-Type: application/json" -d "{\"Name\":\"Alice\"}"   
    
    let private postHandlerAsync : HttpHandler = //type HttpHandler = HttpFunc -> HttpContext -> HttpFuncResult     //GIRAFFE

        fun (next : HttpFunc) (ctx : HttpContext)  //GIRAFFE
            ->
             async
                 {
                     // Read the body of the request as a string
                     use reader = new StreamReader(ctx.Request.Body)
                     let! body = reader.ReadToEndAsync() |> Async.AwaitTask
            
                     let payload = //body serialised by FsHttp
                        Decode.fromString decoderPost body  
                        |> function
                            | Ok value -> value
                            | Error err  -> { Name = err } //To replace with default values in production   

                     (*
                         // If no response content is needed, set the status code to 204
                         ctx.Response.StatusCode <- StatusCodes.Status204NoContent

                         // Return an empty response
                         return! next ctx |> Async.AwaitTask                       
                     *)

                     let response = 
                        {
                             Message = sprintf "Hello, %s!" payload.Name
                        }

                     let responseText = Encode.toString 2 (encoderPost response)
                     ctx.Response.ContentType <- "application/json" 

                     return! text responseText next ctx |> Async.AwaitTask //type HttpFuncResult = Task<HttpContext option>  //GIRAFFE
                 }
             |> Async.StartImmediateAsTask

    let private postHandlerTask : HttpHandler =   //GIRAFFE
    
        fun (next : HttpFunc) (ctx : HttpContext)   //GIRAFFE
            ->
            task
                {
                    use reader = new StreamReader(ctx.Request.Body)
                    let! body = reader.ReadToEndAsync() 
                
                    let payload = //body serialised by FsHttp
                        Decode.fromString decoderPost body  
                        |> function
                            | Ok value -> value
                            | Error err  -> { Name = String.Empty } //To replace with default values in production

                    (*
                        // If no response content is needed, set the status code to 204
                        ctx.Response.StatusCode <- StatusCodes.Status204NoContent

                        // Return an empty response
                        return! next ctx                        
                    *)
    
                    let response = 
                        {
                            Message = sprintf "Hello, %s!" payload.Name
                        }
    
                    let responseText = Encode.toString 2 (encoderPost response)
                    ctx.Response.ContentType <- "application/json" 
    
                    return! text responseText next ctx 
                }
                
    // ************** PUT *******************
            
    // DataTable to store user data
    let private usersTable = 

        let dt = new DataTable()

        dt.Columns.Add("Id", typeof<int>) |> ignore
        dt.Columns.Add("Name", typeof<string>) |> ignore
        dt.Columns.Add("Email", typeof<string>) |> ignore
    
        // Set Id column as primary key
        dt.PrimaryKey <- [| dt.Columns.["Id"] |]
     
        // Add some initial data
        dt.Rows.Add(1, "Alice", "alice@example.com") |> ignore
        dt.Rows.Add(2, "Bob", "bob@example.com") |> ignore
        dt
    
    // Handler for PUT request to update user details
    (*
     curl -X PUT http://localhost:8080/user \
     -H "Content-Type: application/json" \
     -d '{"Id":2, "Name":"Robert", "Email":"robert@example.com"}'
    *)
    let private putHandler : HttpHandler =   //GIRAFFE

        fun (next : HttpFunc) (ctx : HttpContext)   //GIRAFFE
            ->
             task
                 {
                     use reader = new StreamReader(ctx.Request.Body)
                     let! body = reader.ReadToEndAsync() 
                
                     try
                         let updatedUser =  //body serialised by FsHttp
                             Decode.fromString decoderPut body  
                             |> function
                                 | Ok value ->
                                             value
                                 | Error err  ->                                         
                                             PyramidOfDoom
                                                 {
                                                     let! value = usersTable.Rows |> Option.ofObj, { Id = -1; Name = String.Empty; Email = String.Empty }
                                                     // Retrieve the first row as a fallback
                                                     let! row = value |> Seq.cast<DataRow> |> Seq.tryHead, { Id = -1; Name = String.Empty; Email = String.Empty }

                                                     return
                                                         { 
                                                             Id = Convert.ToInt32 row.["Id"] //throws exception if conversion fails 
                                                             Name = Convert.ToString row.["Name"]   //Convert.ToString -> value or string empty // |> Option.ofNullEmpty
                                                             Email = err//Convert.ToString row.["Email"] //Convert.ToString -> value or string empty // |> Option.ofNullEmptyy
                                                         }
                                                 }
    
                         // Find the user by ID and update their details in the DataTable
                         let userRow = usersTable.Rows.Find(updatedUser.Id) |> Option.ofObj
    
                         match userRow with
                         | Some userRow 
                             -> 
                              userRow.["Name"] <- updatedUser.Name
                              userRow.["Email"] <- updatedUser.Email  

                              let response = 
                                  {
                                      Message = sprintf "User with ID %d updated successfully." updatedUser.Id
                                      UpdatedDataTableInfo = 
                                          {
                                              Id = Convert.ToInt32 userRow.["Id"] //throws exception if conversion fails 
                                              Name = Convert.ToString userRow.["Name"]   //Convert.ToString -> value or string empty // |> Option.ofNullEmpty
                                              Email = Convert.ToString userRow.["Email"] //Convert.ToString -> value or string empty // |> Option.ofNullEmpty
                                          }
                                  }
    
                              let responseText = Encode.toString 2 (encoderPut response)
                              ctx.Response.ContentType <- "application/json" 
    
                              return! text responseText next ctx    //GIRAFFE

                         | None 
                             ->                         
                              let response = 
                                  {
                                      Message = sprintf "User with ID %d not found." updatedUser.Id
                                      UpdatedDataTableInfo = 
                                          {
                                              Id = -1
                                              Name = String.Empty
                                              Email = String.Empty
                                          }
                                  }
    
                              let responseText = Encode.toString 2 (encoderPut response)
                              ctx.Response.ContentType <- "application/json"
                              ctx.Response.StatusCode <- 404

                              return! text responseText next ctx  //GIRAFFE
                 
                     with
                     | ex -> 
                           ctx.Response.StatusCode <- 400
                           return! text (sprintf "Error: %s" ex.Message) next ctx    //GIRAFFE             
                }
    
    // Router configuration
    let private apiRouter = //SATURN

        router
            {
                get "/" getHandler
                post "/" postHandlerAsync
                //post "/" postHandlerTask
                put "/user" putHandler
            }

    // Application setup
    let private app =  //SATURN

        application
            {
                use_router apiRouter
                url "http://0.0.0.0:8080"
                memory_cache
                use_static "static"
                use_gzip
            }

    let internal runApiThothJson () =  //SATURN
        run app

