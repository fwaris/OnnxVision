module OnnxVision.Server.Index

open Bolero
open Bolero.Html
open Bolero.Server.Html
open OnnxVision

let page = doctypeHtml {
    head {
        meta { attr.charset "UTF-8" }
        meta { attr.name "viewport"; attr.content "width=device-width, initial-scale=1.0" }
        title { "Bolero Application" }
        ``base`` { attr.href "/" }
        link { attr.rel "stylesheet"; attr.href "https://cdnjs.cloudflare.com/ajax/libs/bulma/0.7.4/css/bulma.min.css" }
        link { attr.rel "stylesheet"; attr.href "css/index.css" }
        link { attr.rel "stylesheet"; attr.href "OnnxVision.Client.styles.css" }
    }
    body {
        nav {
            attr.``class`` "navbar is-dark"
            "role" => "navigation"
            attr.aria "label" "main navigation"
            div {
                attr.``class`` "navbar-brand"
                text "OnnxVision"
            }
        }
        div { attr.id "main"; comp<Client.App.MyApp> }
        boleroScript
    }
}
