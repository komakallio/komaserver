# Komakallio roof server

You need to have a Redis instance running in port 6379 for storing runtime data.

To run the server, run:

```bash
npm install
npm start
```

## Examples

```bash
curl -X POST http://localhost:9000/roof/jari/open
```

```bash
curl http://localhost:9000/roof/jari
```

```bash
curl -X POST http://localhost:9000/roof/jari/close
```
