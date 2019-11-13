const Epub = require("epub-gen");

function createEpubRPC(data) {

    const option = {
        title: data.title, // *Required, title of the book.
        author: data.author, // *Required, name of the author.
        publisher: data.publisher, // optional // Url or File path, both ok.
        content: [
            {
                data: "<div lang=\"en\">" + data.text + "</div>" // pass html string
            }
        ]
    };

    new Epub(option, data.file).promise.then(
        () => console.log("Ebook Generated Successfully!"),
        err => console.error("Failed to generate Ebook because of ", err)
    );
}


module.exports = {
    createEpubRPC: createEpubRPC
}