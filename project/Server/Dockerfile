FROM golang:1.27.1-bookworm AS builder

ENV CGO_ENABLED=1
ENV GOTOOLCHAIN=local

WORKDIR /src/nakama
COPY nakama/ ./
RUN go build -trimpath -mod=vendor -ldflags "-X main.version=local" -o /out/nakama .

WORKDIR /src/backend
# Both binaries use the same vendored dependencies and Go compiler.
RUN cp /src/nakama/go.mod /src/nakama/go.sum ./ && cp -a /src/nakama/vendor ./vendor
COPY go.mod /tmp/backend.go.mod
RUN module="$(sed -n 's/^module //p' /tmp/backend.go.mod)" && go mod edit -module="$module"
COPY Gen ./Gen
COPY runtime ./runtime
RUN go build -trimpath -mod=vendor -buildmode=plugin -o /out/backend.so ./runtime

FROM debian:bookworm-slim
RUN apt-get update && apt-get install -y --no-install-recommends ca-certificates tzdata tini \
    && rm -rf /var/lib/apt/lists/*
WORKDIR /nakama
COPY --from=builder /out/nakama /nakama/nakama
COPY --from=builder /out/backend.so /nakama/data/modules/backend.so
COPY Config /nakama/data/modules/runtime/Config
EXPOSE 7349 7350 7351
ENTRYPOINT ["tini", "--", "/nakama/nakama"]
HEALTHCHECK --interval=30s --timeout=10s CMD /nakama/nakama healthcheck || exit 1
