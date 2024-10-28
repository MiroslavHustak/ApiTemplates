﻿namespace RestApiThothJson

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

module ThothJson =

    open System
    open System.IO
    open System.Data
    
    open Saturn
    open Giraffe
    open Microsoft.AspNetCore.Http    

    open Helpers
    open ThothCoders

    let private apiKey = "my-api-key"  

    let private validateApiKey (next : HttpFunc) (ctx: HttpContext) =  //GIRAFFE
                 
        match ctx.Request.Headers.TryGetValue("X-API-KEY") with
        | true, key 
            when string key = apiKey 
                -> 
                 next ctx  
        | _     ->
                 ctx.Response.StatusCode <- 401
                 ctx.Response.WriteAsync("Unauthorized: Invalid API Key") |> ignore
                 System.Threading.Tasks.Task.FromResult<HttpContext option>(None) // API key is missing or invalid
    
    // ************** GET *******************
           
    // curl -X GET http://localhost:8080/    
    let private getHandler : HttpHandler =  //GIRAFFE        

        fun (next : HttpFunc) (ctx : HttpContext) -> 
            async
                {
                    let response = 
                        {
                            Message = "Hello, World!"
                            Timestamp = System.DateTime.UtcNow.ToString("o") // ISO 8601 format
                        }

                    let responseJson = Encode.toString 2 (encoderGet response) 
                    ctx.Response.ContentType <- "application/json"

                    // Return the response
                    return! text responseJson next ctx |> Async.AwaitTask  //GIRAFFE
                }
            |> Async.StartImmediateAsTask 

    // Handler for GET request with parameter sent via URL  
    //****************************************************
    let private getHandlerWithParam : HttpHandler = //GIRAFFE

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

                    let responseJson = Encode.toString 2 (encoderGet response) 
                    ctx.Response.ContentType <- "application/json"

                    // Return the response
                    return! text responseJson next ctx |> Async.AwaitTask  //GIRAFFE
                }
            |> Async.StartImmediateAsTask
    
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
                            | Ok value  -> value
                            | Error err -> { Name = err } //String.Empty //To be replaced with default values in production   

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
                            | Error _  -> { Name = String.Empty } //To be replaced with default values in production  

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
                                 | Error _  ->                                         
                                             PyramidOfDoom
                                                 {
                                                     let! value = usersTable.Rows |> Option.ofObj, { Id = -1; Name = String.Empty; Email = String.Empty }
                                                     // Retrieve the first row as a fallback
                                                     let! row = value |> Seq.cast<DataRow> |> Seq.tryHead, { Id = -1; Name = String.Empty; Email = String.Empty }

                                                     return
                                                         { 
                                                             Id = Convert.ToInt32 row.["Id"] //throws exception if conversion fails 
                                                             Name = Convert.ToString row.["Name"]   //Convert.ToString -> value or string empty // |> Option.ofNullEmpty
                                                             Email = Convert.ToString row.["Email"] //Convert.ToString -> value or string empty // |> Option.ofNullEmptyy
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
                pipe_through validateApiKey //...for every request
                get "/" getHandler   
                get "/api/greetings/greet" getHandlerWithParam
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

    let internal runApiThothJson () =  //SATURN
        run app

(*
HTTP Methods: REST APIs use standard HTTP methods (GET, POST, PUT, DELETE, etc.) to interact with resources:

GET to retrieve data.
POST to create new resources.
PUT to update existing resources.
DELETE to remove resources.

RESTful APIs are designed to work with resources and their representations. 
REST emphasizes stateless operations and standard HTTP methods (GET, POST, PUT, DELETE) to manipulate resources.
REST APIs primarily use HTTP and its methods (GET, POST, PUT, DELETE). 
RESTful services are inherently stateless and typically use JSON or XML for data representation.
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

(*
PUT Endpoint:

URL: /
Method: PUT
Handler: putHandler
Description: This endpoint responds to HTTP PUT requests by accepting a JSON payload that represents the complete update of a resource. It deserializes the payload and returns a confirmation message indicating that the resource has been updated.

*****************************************************************************************************

PATCH Endpoint:

URL: /
Method: PATCH
Handler: patchHandler
Description: This endpoint responds to HTTP PATCH requests by accepting a JSON payload with partial data that updates a resource. It deserializes the payload and returns a confirmation message indicating that the resource has been partially updated.

*****************************************************************************************************

DELETE Endpoint:

URL: /
Method: DELETE
Handler: deleteHandler
Description: This endpoint responds to HTTP DELETE requests by accepting a resource identifier (e.g., an ID) and returns a confirmation message indicating that the specified resource has been deleted.
*)

(*
1. Preparing the Request
a. Building the Request
Choose the HTTP Method: Depending on the operation, you choose a method (e.g., GET, POST, PUT, DELETE).
Set the URL: Define the URL endpoint where the request is being sent.
Headers: Add any necessary headers, like Content-Type (to specify the format of the data you're sending, such as JSON), Authorization (for authentication tokens), etc.
Body (if applicable): For methods like POST, PUT, and PATCH, you serialize the data into the appropriate format (e.g., JSON, XML) and include it in the request body.

2. Sending the Request
a. Opening a Network Connection
The HTTP client (e.g., browser, mobile app, or a program like your F# code) establishes a TCP/IP connection to the server's IP address and port (usually port 80 for HTTP or port 443 for HTTPS).
b. Transmitting the Request
The HTTP request is packaged into a format that can be transmitted over the network. This includes:
The request line (method, URL, HTTP version).
The headers.
The body (if applicable).
The client then sends this request data over the network to the server via the established connection.

3. Network Layer Handling
a. DNS Resolution
If you provide a domain name (like example.com), your client needs to resolve this to an IP address using DNS (Domain Name System).
b. Transport Layer (TCP/IP)
The request data is split into smaller packets at the transport layer (TCP), which ensures that data is sent reliably.
These packets are then routed over the internet to the destination server using IP.
c. Security Layer (TLS/SSL)
If you’re using HTTPS, the data is encrypted using TLS/SSL to protect it from being intercepted and read by third parties.

4. Server-Side Handling
a. Receiving the Request
The server receives the TCP packets, reassembles them, and passes the HTTP request to the web server software (e.g., Apache, Nginx, or the ASP.NET Core server).
b. Processing the Request
The server's web framework (e.g., Giraffe for F#, Express for Node.js) processes the request:
It reads the URL to determine which route or controller to use.
It processes headers and the request body.
It performs the requested operation (e.g., querying a database, performing calculations).

5. Server Response
a. Creating the Response
After processing the request, the server creates an HTTP response.
Status Code: Indicates the result (e.g., 200 for success, 404 for not found).
Headers: Additional information like Content-Type.
Body: The actual data (e.g., HTML, JSON) returned to the client.
b. Sending the Response
The server sends the response back through the network, again as TCP packets, which are reassembled on the client-side.

6. Client Handling of the Response
a. Receiving and Reassembling the Response
The client receives the TCP packets, reassembles them into the full HTTP response.
b. Processing the Response
The client application reads the response status, headers, and body.
If the request was successful, the client might display the data, update the UI, or process the data further.
If there was an error (e.g., a 404 Not Found), the client handles it accordingly, such as showing an error message.

*)