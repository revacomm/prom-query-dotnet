# Prom Query SDK [<img src=https://www.cncf.io/wp-content/uploads/2020/08/prometheusBanner-1.png width="75" height="23"/>](https://www.cncf.io/wp-content/uploads/2020/08/prometheusBanner-1.png)

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

