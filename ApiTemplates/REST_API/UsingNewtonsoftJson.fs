namespace RestApiNewtonsoftJson

//Templates -> try-with blocks and Option/Result to be added when used in production

//REST API created with SATURN and GIRAFFE
//Data format -> JSON
//Client Library -> FsHttp 
//(De)Serialization -> Newtonsoft.Json

module NewtonsoftJson =

    open System
    open System.IO
    open System.Data
    
    open Saturn
    open Giraffe
    open Newtonsoft.Json  
    open Microsoft.AspNetCore.Http    

    open Helpers       
    
    // ************** GET *******************
        
    // Define the response type for GET request    
    type HelloResponseGet = 
        {
            Message : string
            Timestamp : string
        }

    // Handler for GET request
    // curl -X GET http://localhost:8080/    
    let private getHandler : HttpHandler =      
             
        fun (next : HttpFunc) (ctx : HttpContext) ->   //GIRAFFE
            async
                {
                    // Extract the "name" query parameter from the URL
                    let name = 
                        string ctx.Request.Query.["name"] |> Option.ofNullEmptySpace
                        |> function
                            | Some value -> value
                            | None       -> "Guest"                    

                    let response = 
                        {
                            Message = "Hello, World!"
                            Timestamp = System.DateTime.UtcNow.ToString("o") // ISO 8601 format
                        }

                    let responseJson = JsonConvert.SerializeObject(response)
                    ctx.Response.ContentType <- "application/json"

                    // Return the response
                    return! text responseJson next ctx |> Async.AwaitTask  //GIRAFFE
                }
            |> Async.StartImmediateAsTask
           
    // Handler for GET request with parameter sent via URL  
    //****************************************************
    let private getHandlerParam : HttpHandler = 

        fun (next : HttpFunc) (ctx : HttpContext) -> 
            async
                {
                    // Extract the "name" query parameter from the URL
                    let name = 
                        string ctx.Request.Query.["name"] |> Option.ofNullEmptySpace
                        |> function
                            | Some value -> value
                            | None       -> "Guest"                    

                    let response = 
                        {
                            Message = sprintf "Hello, %s!" name
                            Timestamp = System.DateTime.UtcNow.ToString("o") // ISO 8601 format
                        }

                    let responseJson = JsonConvert.SerializeObject(response)
                    ctx.Response.ContentType <- "application/json"

                    // Return the response
                    return! text responseJson next ctx |> Async.AwaitTask
                }
            |> Async.StartImmediateAsTask
   
    
    // ************** POST *******************

    // Payload type 
    type HelloPayload =
        {
            Name: string
        }

    // Response type for POST request 
    type HelloResponsePost = 
        {
            Message: string
        }  

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
            
                     // Deserialize using Newtonsoft.Json
                     let payload = JsonConvert.DeserializeObject<HelloPayload>(body)

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

                     let responseText = JsonConvert.SerializeObject(response)
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
                
                     let payload = JsonConvert.DeserializeObject<HelloPayload>(body)

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
    
                     let responseText = JsonConvert.SerializeObject(response)
                     ctx.Response.ContentType <- "application/json" 
    
                     return! text responseText next ctx 
                 }
                
    // ************** PUT *******************
    
    // Payload type
    type UserPayload =
        {
            Id : int
            Name : string
            Email : string
        }
    
    // Response type
    type UserResponsePut = 
        {
            Message: string
            UpdatedDataTableInfo: UserPayload
        }
    
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
                         let updatedUser = JsonConvert.DeserializeObject<UserPayload>(body)
    
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
    
                              let responseText = JsonConvert.SerializeObject(response)
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
    
                              let responseText = JsonConvert.SerializeObject(response)
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
                get "/api/greetings/greet" getHandlerParam
                //post "/" postHandlerAsync
                //post "/" postHandlerTask
                post "/api/greetings/greet" postHandlerAsync
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

    let internal runApiNewtonsoftJson () =  //SATURN
        run app