# Prom Query SDK

## Overview
Prom Query SDK is a C# SDK for the Prometheus HTTP API

#### Documentation Reference
https://prometheus.io/docs/prometheus/latest/querying/api/

## Usage
#### Example code for creating the client
  ```
  var queryClient = new PrometheusClient(CreateHttpClient);
  var response = await queryClient.QueryRangeAsync(query, start, end, step, null, cancellationToken);
  ```

## Methods
- Expression Queries
  - Instant Queries
  - Range Queries
- Metadata Queries
  - Series
  - Label Name
  - Label Value

## Contributors
[@RidgeRW](https://github.com/RidgeRW)

