# Parallel Web Crawler

This is a parallel web crawler developed in F# using the actor model.

## Build and Run

The app can be built and run using Visual Studio. The provided source code contains Visual Studio 2017 solution and project.

Following command line switches are supported by the app:

```
crawler \
    --max-depth <number> \
    --output <folder> \
    --scale <number> \
    <input_file>
```

Where, all except the `input_file` is optional.

* `max-depth` is the maximum depth the crawler can go before stopping. Defaults to 2.
* `output` is the folder where the pages are downloaded. If not provided, the pages are not downloaded.
* `scale` is the factor to scale the number of workers. This is multiplied with the number of processors available in the system. Defaults to 2.
* `input_file` is a files containing the list of seed urls in separate lines.

### Example usage

```
crawler --max-depth 3 --output out seed-urls.txt
```
