const Epub = require("epub-gen");

function createEpub(data) {
    
    content = [];
    data.chapters.forEach(function (chapter, index) {
        content.push({
            title: `Chapter ${index+1}`,
            data: `
                <img src="${chapter.image.filePath}" />
                <p lang="en">${chapter.text}</p>
            `
        })
    });
    console.log(content[3]);
    const option = {
        title: data.title, // *Required, title of the book.
        author: data.author, // *Required, name of the author.
        publisher: data.publisher, // optional // Url or File path, both ok.
        content: content
    };

    new Epub(option, data.file).promise.then(
        () => console.log("Ebook Generated Successfully!"),
        err => console.error("Failed to generate Ebook because of ", err)
    );
    return data.file;
}

function createEpubRPC(call, callback) {
    var outputFile = createEpub(call.request);
    callback(null, { filePath: outputFile });
}

module.exports = {
    createEpubRPC: createEpubRPC
}