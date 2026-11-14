#:package Bogus@35.6.5
using Bogus;

var faker = new Faker();
for (var i = 0; i < 3; i++)
    Console.WriteLine(faker.Name.FullName());
