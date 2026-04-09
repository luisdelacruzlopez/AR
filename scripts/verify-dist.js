const fs = require("fs");
const path = require("path");

const indexPath = path.join(__dirname, "..", "apphosting_dist", "wwwroot", "index.html");

if (!fs.existsSync(indexPath)) {
  console.error("Missing apphosting_dist/wwwroot/index.html");
  console.error("Run 'npm run prepare:dist' before deploying to Firebase App Hosting.");
  process.exit(1);
}

console.log("Build artifacts found in apphosting_dist/wwwroot");
