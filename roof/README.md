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

## Edge cases to be fixed

1. Open during CLOSING is silently lost
2. Close during OPENING leaves the physical roof open but user sees CLOSED
3. When the physical roof is opening, closing, errored, or stopped, the "virtual roof" reported state is the same, even if the virtual roof is closed or open.
4. When Redis has no state, JSON.parse(null) || defaultRoofState returns the same object reference. The middleware and state callbacks then mutate it directly (pushing to arrays, setting properties). On the next request where Redis is empty, defaultRoofState is already polluted with stale data.
5. In the close handler (line 211-213), otherUsers only checks users — not openRequestedBy. If user A has the roof open and user B's open request is still pending in openRequestedBy (during OPENING), and A closes (hitting the OPENING case, line 227), A is removed and B stays in openRequestedBy. This particular path works by accident, but if the state were OPEN and B were somehow still in openRequestedBy rather than users (due to race condition #4), A closing would trigger a physical close despite B's pending request.
