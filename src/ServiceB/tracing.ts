import { NodeSDK } from '@opentelemetry/sdk-node';
import { getNodeAutoInstrumentations } from '@opentelemetry/auto-instrumentations-node';
// import { OTLPTraceExporter } from '@opentelemetry/exporter-trace-otlp-grpc';
import { OTLPTraceExporter } from '@opentelemetry/exporter-trace-otlp-proto';

// const sdk = new NodeSDK({
//   traceExporter: new OTLPTraceExporter({
//     url: 'grpc://jaeger:4317',
//   }),
//   instrumentations: [getNodeAutoInstrumentations()],
//   serviceName: 'Service-B',
// });

const sdk = new NodeSDK({
  traceExporter: new OTLPTraceExporter({
    url: 'http://jaeger:4318/v1/traces', // ใช้พอร์ต 4318 และระบุ Path
  }),
  instrumentations: [getNodeAutoInstrumentations()],
  serviceName: 'Service-B',
});

sdk.start();