# HackerNews Solution

This repo contains a solution for the following problem:

Download the top the stories from the Hacker News API: https://github.com/HackerNews/API

For each of the top 30 stories, we want to have an output containing:

- The story title

- The top 10 commenters of that story.

For each commenter:

- The number of comments they made on the story.

- The total number of comments they made among all the top 30 stories.

For instance, if we consider just the 3 top stories (instead of 30) and top 2 commenters (instead of 10):

| Story A | Story B | Story C |
|--------------------|---------------------|---------------------|
| user-a (1 comment) | user-a (4 comments) | user-a (4 comments) |
| user-b (2 comment) | user-b (3 comments) | user-b (5 comments) |
| user-c (3 comment) | user-c (2 comments) | user-c (3 comments) |

We want the output to look as follows:

| Story | 1st Top Commenter | 2nd Top Commenter |
|---------|---------------------------------|---------------------------------|
| Story A | user-c (3 for story - 8 total) | user-b (2 for story - 10 total) |

# Run the solution locally
## With Docker

You can run the `run.sh` script.

## On your machine

1. Download the .Net Core SDK or run time here(https://dotnet.microsoft.com/download)
2. `cd HackerNews`
3. `dotnet build`
4. `dotnet run`

# Run the tests

1. `cd Tests`
2. `dotnet build`
2. `dotnet test`
