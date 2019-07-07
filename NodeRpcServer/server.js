    
/*
 *
 * Copyright 2015 gRPC authors.
 *
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at
 *
 *     http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 *
 */

var PROTO_PATH = __dirname + '/ct.proto';
var _ = require('lodash');
var grpc = require('grpc');
var protoLoader = require('@grpc/proto-loader');
var parseArgs = require('minimist');
var path = require('path');
var echo = require('./echo');
var youtube = require('./youtube');

var packageDefinition = protoLoader.loadSync(
    PROTO_PATH,
    {keepCase: true,
     longs: String,
     enums: String,
     defaults: true,
     oneofs: true
    });

var CTGrpc = grpc.loadPackageDefinition(packageDefinition).CTGrpc;

/**
 * Starts an RPC server that receives requests for the Greeter service at the
 * sample server port
 */
function main() {
   var server = new grpc.Server();
   server.addService(CTGrpc.NodeServer.service, 
     {
       getEchoPlaylist: echo.getEchoPlaylist, 
       getEchoVideo: echo.getEchoVideo,
       getYoutubePlaylist: youtube.getYoutubePlaylist, 
       getYoutubeVideo: youtube.getYoutubeVideo
     });
   server.bind('0.0.0.0:50052', grpc.ServerCredentials.createInsecure());
   server.start();
   console.log("NodeRpcServer Started!");
  // echo.downloadEchoLecture("e9053773-74cf-45a2-b3e8-5feac76a4a97", "https://content.echo360.org/bee3.d422df75-c848-4e5e-b375-e46893d4de8a/d88cd0ee-b33b-40e5-9b93-0968735a6470/hd1.mp4", "Cookie: CloudFront-Key-Pair-Id=APKAIPMYRDQXV3PXG2XA; CloudFront-Policy=eyJTdGF0ZW1lbnQiOiBbeyJSZXNvdXJjZSI6Imh0dHBzOi8vKi5lY2hvMzYwLm9yZy8qZDQyMmRmNzUtYzg0OC00ZTVlLWIzNzUtZTQ2ODkzZDRkZThhLyoiLCJDb25kaXRpb24iOnsiRGF0ZUxlc3NUaGFuIjp7IkFXUzpFcG9jaFRpbWUiOjE1NjI0NzE0MzV9fX1dfQ__; CloudFront-Signature=UTHImjtwNr-2sWWMEuRr83M5piL4QNet2FdjzJx3GDKHlv9cxBdtO1N8wRAUOKFXdRHGGIt-49QWwvBVyZLvDeQzDMWlOJL3sj1543sFMi6y-iqUoqPLVYtwbeqOa0uwcUgoAqeHRKkX1ZcMusBEeAZknPc-MeeqCrgBjCVoZFALkJtK41e8jnTUtqsMgs3tfuyoG~bt~6~C2E1w0HfOL9XFLVmT2QbOF8wxuCEgtgvCXOCrVqJGEuLyYrVOBcdQo-idoxX3-WltkQ7-4Xv7YCJMdtF4eX87M6qCcJMaY2DglvDncZlEujdpW9X75KmfQZAohl3HS9ugBIo6TKsMtQ__");
  // youtube.downloadYoutubeVideo("7e7e7179-1289-4899-b075-1374149502d6", "http://www.youtube.com/watch?v=uzYxh7iGCIM")
}

main();