# Pin to the same Jellyfin version as the plugin's NuGet ABI target
FROM jellyfin/jellyfin:latest AS jf-web

FROM node:alpine
WORKDIR /app
COPY server.js .
COPY wwwroot/ ./wwwroot/
# fixtures/ is mounted at runtime; copy defaults so the image works standalone
COPY fixtures/ ./fixtures/
# Bake Jellyfin's web assets in so /web/* serves real Jellyfin CSS/fonts
COPY --from=jf-web /jellyfin/jellyfin-web /app/jellyfin-web
EXPOSE 8080
CMD ["node", "server.js"]
