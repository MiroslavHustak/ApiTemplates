namespace RestApiSystemTextJson

//Templates -> try-with blocks to be added when used in production

//REST API
//Data format -> JSON
//Client Library -> FsHttp 
//(De)Serialization -> System.Text.Json

module RestApiTextJson =
    
    open System
    open System.IO
    open System.Data

    open Saturn
    open Giraffe    
    open System.Text.Json
    open Microsoft.AspNetCore.Http

    // ************** GET *******************

    // Response type for GET request
    type HelloResponse = 
        {
            Message: string
            Timestamp: string
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
             let jsonResponse = JsonSerializer.Serialize(response)
             
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

    // Handler for POST request
    // curl -X POST http://localhost:8080/ -H "Content-Type: application/json" -d "{\"Name\":\"Alice\"}"   
    let private postHandler : HttpHandler =

        fun (next : HttpFunc) (ctx : HttpContext)
            ->
             task
                 {
                     // Read the body of the request as a string
                     use reader = new StreamReader(ctx.Request.Body)
                     let! body = reader.ReadToEndAsync()
                
                     // Deserialize using System.Text.Json
                     let options = JsonSerializerOptions(PropertyNameCaseInsensitive = true)
                     let payload = JsonSerializer.Deserialize<HelloPayload>(body, options)

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
                     
                     let jsonResponse = JsonSerializer.Serialize(response)
                     ctx.Response.ContentType <- "application/json"
                     
                     return! text jsonResponse next ctx
                 }

    // ************** PUT *******************
       
    // Payload type
    type UserPayload =
        {
            Id: int
            Name: string
            Email: string
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
                
                    let options = JsonSerializerOptions(PropertyNameCaseInsensitive = true)
                    try
                        let updatedUser = JsonSerializer.Deserialize<UserPayload>(body, options)
    
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
    
                            let jsonResponse = JsonSerializer.Serialize(response)
                            ctx.Response.ContentType <- "application/json"

                            return! text jsonResponse next ctx

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
    
                            let jsonResponse = JsonSerializer.Serialize(response)
                            ctx.Response.ContentType <- "application/json"
                            ctx.Response.StatusCode <- 404

                            return! text jsonResponse next ctx
                    
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
                post "/" postHandler
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

    let internal runApiTextJson () =
        run app

