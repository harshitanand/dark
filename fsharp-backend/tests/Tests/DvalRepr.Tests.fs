module Tests.DvalRepr

open Expecto
open Prelude
open Prelude.Tablecloth
open TestUtils

module PT = LibBackend.ProgramTypes
module RT = LibExecution.RuntimeTypes

module DvalRepr = LibExecution.DvalRepr


let testInternalRoundtrippableDoesntCareAboutOrder =
  test "internal_roundtrippable doesn't care about key order" {
    Expect.equal
      (DvalRepr.ofInternalRoundtrippableV0
        "{
           \"type\": \"option\",
           \"value\": 5
          }")
      (DvalRepr.ofInternalRoundtrippableV0
        "{
           \"value\": 5,
           \"type\": \"option\"
          }")
      ""
  }


let testDvalRoundtrippableRoundtrips =
  testMany
    "special roundtrippable dvals roundtrip"
    FuzzTests.All.Roundtrippable.roundtrip
    [ RT.DObj(
        Map.ofList [ ("", RT.DFloat 1.797693135e+308)
                     ("a", RT.DErrorRail(RT.DFloat nan)) ]
      ),
      true ]


let testDvalOptionQueryableSpecialCase =
  test "dval Option Queryable Special Case" {
    let dvm = Map.ofList [ ("type", RT.DStr "option"); ("value", RT.DInt 5I) ]

    Expect.equal
      (RT.DObj dvm)
      (dvm |> DvalRepr.toInternalQueryableV1 |> DvalRepr.ofInternalQueryableV1)
      "extra"
  }

let testToDeveloperRepr =
  testList
    "toDeveloperRepr"
    [ testMany
        "toDeveloperRepr string"
        DvalRepr.toDeveloperReprV0
        // Most of this is just the OCaml output and not really what the output should be
        [ RT.DHttpResponse(RT.Response(0, []), RT.DNull), "0 {  }\nnull"
          RT.DFloat(-0.0), "-0."
          RT.DFloat(infinity), "inf"
          RT.DObj(Map.ofList [ "", RT.DNull ]), "{ \n  : null\n}"
          RT.DList [ RT.DNull ], "[ \n  null\n]" ] ]

let testToEnduserReadable =
  testList
    "enduserReadable"
    [ testMany
        "toEnduserReadable string"
        DvalRepr.toEnduserReadableTextV0
        // Most of this is just the OCaml output and not really what the output should be
        [ RT.DFloat(0.0), "0." // this type of thing in particular is ridic
          RT.DFloat(-0.0), "-0."
          RT.DFloat(5.0), "5."
          RT.DFloat(5.1), "5.1"
          RT.DFloat(-5.0), "-5."
          RT.DFloat(-5.1), "-5.1"
          RT.DError(RT.SourceNone, "Some message"), "Error: Some message"
          RT.DHttpResponse(RT.Redirect("some url"), RT.DNull), "302 some url\nnull"
          RT.DHttpResponse(RT.Response(0, [ "a header", "something" ]), RT.DNull),
          "0 { a header: something }\nnull" ] ]

module F = FuzzTests.All

let allRoundtrips =
  let t = testListUsingProperty
  let all = TestUtils.sampleDvals
  let dvs (filter : RT.Dval -> bool) = List.filter (fun (_, dv) -> filter dv) all

  testList
    "roundtrips"
    [ t
        "roundtrippable"
        F.Roundtrippable.roundtrip
        (dvs (DvalRepr.isRoundtrippableDval false))
      t
        "roundtrippable interop"
        F.Roundtrippable.isInteroperableV0
        (dvs (DvalRepr.isRoundtrippableDval false))
      t "queryable v0" F.Queryable.v1Roundtrip (dvs DvalRepr.isQueryableDval)
      t
        "queryable interop v0"
        F.Queryable.isInteroperableV0
        (dvs DvalRepr.isQueryableDval)
      t
        "queryable interop v1"
        F.Queryable.isInteroperableV1
        (dvs DvalRepr.isQueryableDval)
      t "enduserReadable" F.EndUserReadable.equalsOCaml all
      t "developerRepr" F.DeveloperRepr.equalsOCaml all
      t "prettyMachineJson" F.PrettyMachineJson.equalsOCaml all ]

// let testDateMigrationHasCorrectFormats () =
//   let str = "2019-03-08T08:26:14Z" in
//   let date = RT.DDate(System.DateTime.ofIsoString str) in
//   let oldFormat = $"{{ \"type\": \"date\", \"value\": \"{str}\"}}"
//   Expect.equal (Legacy.toPrettyMachineJsonStringV0 date) oldFormat "old version"
//   Expect.equal (DvalRepr.toPrettyMachineJsonStringV1 date) $"\"{str}\"" "new version"
//

// let t_password_json_round_trip_forwards () =
//   let password = DPassword (Bytes.of_string "x") in
//   check_dval
//     "Passwords serialize and deserialize if there's no redaction."
//     password
//     ( password
//     |> Dval.to_internal_roundtrippable_v0
//     |> Dval.of_internal_roundtrippable_v0
//     |> Dval.to_internal_roundtrippable_v0
//     |> Dval.of_internal_roundtrippable_v0 )
//
//
// let t_password_serialization () =
//   let does_serialize name expected f =
//     let bytes = Bytes.of_string "encryptedbytes" in
//     let password = DPassword bytes in
//     AT.check
//       AT.bool
//       ("Passwords serialize in non-redaction function: " ^ name)
//       expected
//       (String.is_substring
//          ~substring:(B64.encode "encryptedbytes")
//          (f password))
//   in
//   let roundtrips name serialize deserialize =
//     let bytes = Bytes.of_string "encryptedbytes" in
//     let password = DPassword bytes in
//     AT.check
//       at_dval
//       ("Passwords serialize in non-redaction function: " ^ name)
//       password
//       (password |> serialize |> deserialize |> serialize |> deserialize)
//   in
//   (* doesn't redact *)
//   does_serialize
//     "to_internal_roundtrippable_v0"
//     true
//     Dval.to_internal_roundtrippable_v0 ;
//   (* roundtrips *)
//   roundtrips
//     "to_internal_roundtrippable_v0"
//     Dval.to_internal_roundtrippable_v0
//     Dval.of_internal_roundtrippable_v0 ;
//   (* redacting *)
//   does_serialize
//     "to_enduser_readable_text_v0"
//     false
//     Dval.to_enduser_readable_text_v0 ;
//   does_serialize
//     "to_enduser_readable_html_v0"
//     false
//     Dval.to_enduser_readable_html_v0 ;
//   does_serialize "to_developer_repr_v0" false Dval.to_developer_repr_v0 ;
//   does_serialize
//     "to_pretty_machine_json_v1"
//     false
//     Dval.to_pretty_machine_json_v1 ;
//   does_serialize
//     "to_pretty_request_json_v0"
//     false
//     Libexecution.Legacy.PrettyRequestJsonV0.to_pretty_request_json_v0 ;
//   does_serialize
//     "to_pretty_response_json_v1"
//     false
//     Libexecution.Legacy.PrettyResponseJsonV0.to_pretty_response_json_v0 ;
//   ()
//
//
// (* put it in an object too *)
// let t_password_serialization2 () =
//   let roundtrips name serialize deserialize =
//     let bytes = Bytes.of_string "encryptedbytes" in
//     let password = DObj (DvalMap.singleton "x" (DPassword bytes)) in
//     let wrapped_serialize dval =
//       dval
//       |> (function
//            | DObj dval_map ->
//                dval_map
//            | _ ->
//                Exception.internal "dobj only here")
//       |> serialize
//     in
//     AT.check
//       at_dval
//       ("Passwords serialize in non-redaction function: " ^ name)
//       password
//       ( password
//       |> wrapped_serialize
//       |> deserialize
//       |> wrapped_serialize
//       |> deserialize )
//   in
//   (* roundtrips *)
//   roundtrips
//     "to_internal_queryable_v1"
//     Dval.to_internal_queryable_v1
//     Dval.of_internal_queryable_v1 ;
//   ()

let tests =
  testList
    "dvalRepr"
    [ testDvalRoundtrippableRoundtrips
      testInternalRoundtrippableDoesntCareAboutOrder
      testDvalOptionQueryableSpecialCase
      testToDeveloperRepr
      testToEnduserReadable
      allRoundtrips ]
