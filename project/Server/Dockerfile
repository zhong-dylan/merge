FROM heroiclabs/nakama-pluginbuilder:3.22.0 AS builder

ENV CGO_ENABLED=1

WORKDIR /backend

COPY go.mod ./
RUN go mod download

COPY runtime ./runtime
RUN go mod tidy

RUN go build --trimpath --buildmode=plugin -o ./backend.so ./runtime

FROM heroiclabs/nakama:3.22.0

COPY --from=builder /backend/backend.so /nakama/data/modules/backend.so
