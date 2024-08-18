namespace RestApiThothJson

//Compiler-independent template suitable for Shared as well

//************************************************

//Compiler directives
#if FABLE_COMPILER
open Thoth.Json
#else
open Thoth.Json.Net
#endif

//**************** GET ********************

type HelloResponseGet = 
    {
        Message : string
        Timestamp : string
    } 

//**************** POST ********************

type HelloPayload =
    {
        Name: string
    }

type HelloResponsePost = 
    {
        Message: string
    }  

//**************** PUT ********************

type UserPayload =
    {
        Id : int
        Name : string
        Email : string
    }

type UserResponsePut = 
    {
        Message: string
        UpdatedDataTableInfo: UserPayload
    }

module ThothCoders =   

    //**************** GET ********************

    let internal encoderGet (result : HelloResponseGet) =
        Encode.object
            [
                "Message", Encode.string result.Message
                "Timestamp", Encode.string result.Timestamp
            ]

    //**************** POST ********************

    let internal encoderPost (result : HelloResponsePost) =
        Encode.object
            [
                "Message", Encode.string result.Message
            ]
    
    (*
       In F#, the record fields are typically named using PascalCase (e.g., Name, Email). 
       However, many JSON serializers, including those used by libraries like FsHttp (which internally uses Newtonsoft.Json or System.Text.Json), 
       default to serializing fields in camelCase to match common JSON naming conventions.
      
      json:
       {
         "name": "Alice"
       }   
       
       But it is not like that the other way round:

       Deserialization process used by FsHttp does not require an exact case match between the JSON keys and the F# record properties. 
       This feature of being case-insensitive makes it easier to work with JSON data that might have different naming conventions.

    *)  

    let internal decoderPost : Decoder<HelloPayload> =
        Decode.object
            (fun get ->
                      {
                          Name = get.Required.Field "name" Decode.string //Name = get.Required.Field "Name" Decode.string //"Name" -> Error
                          //Name = get.Required.Raw (Decode.field "Name" Decode.string)  
                      }
            )

    //**************** PUT ********************

    let internal encoderPut (result : UserResponsePut) =
        Encode.object
            [
                "Message", Encode.string result.Message               
                "UpdatedDataTableInfo",
                    Encode.object
                        [
                            "Id", Encode.int result.UpdatedDataTableInfo.Id
                            "Name", Encode.string result.UpdatedDataTableInfo.Name
                            "Email", Encode.string result.UpdatedDataTableInfo.Email
                        ]
            ]

    let internal decoderPut : Decoder<UserPayload> =
        Decode.object
            (fun get ->
                      {
                          (*
                          Id = get.Required.Field "Id" Decode.int
                          Name = get.Required.Field "Name" Decode.string
                          Email = get.Required.Field "Email" Decode.string
                          *)
                          Id = get.Required.Field "id" Decode.int
                          Name = get.Required.Field "name" Decode.string
                          Email = get.Required.Field "email" Decode.string
                      }
            )