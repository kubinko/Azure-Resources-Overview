function upperCase(text){
    return text.toUpperCase();
}

function regexMatch(input, pattern) {
    return input.match(pattern) !== null;
}

function seed(peopleCount, city) {
    var collection = getContext().getCollection();

    var firstNames = ["Adam", "Cyril", "Dávid", "Jakub", "Karol", "Lukáš", "Marek", "Peter", "Štefan", "Vladimír"];
    var lastNames = ["Veselý", "Hrdý", "Pekný", "Podivný", "Pomalý", "Trasľavý", "Ospalý", "Mocný", "Chudý", "Lakomý"];

    console.log("Seed started...");

    var count = 0;
    createPerson();

    function createPerson() {
        if (count == peopleCount) {
            console.log("Seed successfully finished.");

            const response = getContext().getResponse();
            response.status = 200;
            response.setBody("Created " + count + " new records.");

            return;
        }

        console.log("Seeding person " + (count + 1) + "...");

        const name = firstNames[Math.floor(Math.random() * 10)];
        const surname = lastNames[Math.floor(Math.random() * 10)];

        var doc = {
            name: name,
            surname: surname,
            city: city,
            email: surname.toLowerCase() + "." + name.toLowerCase() + "@kros.sk"
        };

        success = collection.createDocument(
            collection.getSelfLink(),
            doc,
            callback);
    }
    
    function callback(err, item, options) {
        if (err) {
            console.log("Seeding encountered an error.");

            const response = getContext().getResponse();
            response.status = 500;
            response.setBody("Error encountered while seeding.");
            throw new Error('Error' + err.message);
        } else {
            count++;
            createPerson();
        }
    }
}

function validityTrigger() {
    const request = getContext().getRequest();
    var person = request.getBody();

    if (!("validuntil" in person)) {
        person["validuntil"] = Date.now() + 365 * 24 * 60 * 60 * 1000;
    }

    request.setBody(person);
}