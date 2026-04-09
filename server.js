const express = require("express");
const path = require("path");

const app = express();
const port = process.env.PORT || 8080;

const webRoot = path.join(__dirname, "apphosting_dist", "wwwroot");

app.use(express.static(webRoot, {
  maxAge: "1h"
}));

app.get("*", (_req, res) => {
  res.sendFile(path.join(webRoot, "index.html"));
});

app.listen(port, () => {
  console.log(`EstadioApp listening on port ${port}`);
});
