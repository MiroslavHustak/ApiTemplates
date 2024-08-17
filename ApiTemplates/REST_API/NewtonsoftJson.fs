namespace RestApiNewtonsoftJson

//Templates -> try-with blocks to be added when used in production

//REST API
//Data format -> JSON
//Client Library -> FsHttp 
//(De)Serialization -> Newtonsoft.Json

(*
HTTP Methods: REST APIs use standard HTTP methods (GET, POST, PUT, DELETE, etc.) to interact with resources:

GET to retrieve data.
POST to create new resources.
PUT to update existing resources.
DELETE to remove resources.
*)

(*    
Run Your F# API:
    
Execute the code to start the web server. It will be bound to 0.0.0.0:8080, making it accessible locally and over the network if your firewall settings allow it.
Testing:    
Local Testing: Open a web browser or an API client and navigate to http://localhost:8080 to test your API endpoints.
Network Testing: If testing from another device on the same network, use the IP address of the machine running the API, like http://192.168.1.100:8080.
*)

(*
Web API Configuration: Keep http://0.0.0.0:8080 in RestApi3.runApi() so the server listens on all network interfaces.
Client Requests: Use http://localhost:8080 or http://127.0.0.1:8080 in your client application to make requests to the server.
*)

module NewtonsoftJson =

    (*
    GET Endpoint:
    
    URL: /
    Method: GET
    Handler: getHandler
    Description: This endpoint responds to HTTP GET requests by returning a JSON object with a greeting message and a timestamp.

    *****************************************************************************************************

    POST Endpoint:

    URL: /
    Method: POST
    Handler: postHandler
    Description: This endpoint responds to HTTP POST requests by accepting a JSON payload with a name field, deserializing it, and returning a greeting message with the provided name.
    *)

    open System
    open System.IO
    open System.Data
    
    open Saturn
    open Giraffe
    open Newtonsoft.Json  
    open Microsoft.AspNetCore.Http    
    
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

        fun (next : HttpFunc) (ctx : HttpContext)
            ->
             // Create a response object
             let response = 
                 {
                     Message = "Hello, World!"
                     Timestamp = System.DateTime.UtcNow.ToString("o") // ISO 8601 format
                 }
            
             // Serialize the response object to JSON
             let jsonResponse = JsonConvert.SerializeObject(response)
             ctx.Response.ContentType <- "application/json"
             text jsonResponse next ctx
    
    
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
    
    let private postHandlerAsync : HttpHandler = //type HttpHandler = HttpFunc -> HttpContext -> HttpFuncResult    

        fun (next : HttpFunc) (ctx : HttpContext)
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

                     return! text responseText next ctx |> Async.AwaitTask //type HttpFuncResult = Task<HttpContext option>
                 }
             |> Async.StartImmediateAsTask

    let private postHandlerTask : HttpHandler =   
    
        fun (next : HttpFunc) (ctx : HttpContext)
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
    let usersTable = 

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
    let private putHandler : HttpHandler =
     fun (next : HttpFunc) (ctx : HttpContext)
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
                                         Id = userRow.["Id"] :?> int
                                         Name = userRow.["Name"].ToString()
                                         Email = userRow.["Email"].ToString()
                                     }
                             }
    
                         let responseText = JsonConvert.SerializeObject(response)
                         ctx.Response.ContentType <- "application/json" 
    
                         return! text responseText next ctx 

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

                         return! text responseText next ctx 
                 
                 with
                 | ex -> 
                       ctx.Response.StatusCode <- 400
                       return! text (sprintf "Error: %s" ex.Message) next ctx                
             }
    
    // Router configuration
    let private apiRouter =

        router
            {
                get "/" getHandler
                post "/" postHandlerAsync
                //post "/" postHandlerTask
                put "/user" putHandler
            }

    // Application setup
    let private app =

        application
            {
                use_router apiRouter
                url "http://0.0.0.0:8080"
                memory_cache
                use_static "static"
                use_gzip
            }

    let internal runApiNewtonsoftJson () =
        run app