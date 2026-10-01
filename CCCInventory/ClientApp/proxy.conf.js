const PROXY_CONFIG = [
  {
    context: ["/api"],
    // Generate Contract makes many Google calls (and uploads photos), so allow up to 2 minutes
    proxyTimeout: 120000,
    target: 'https://localhost:7005',
    secure: false,
    headers: {
      Connection: 'Keep-Alive'
    }
  }
]

module.exports = PROXY_CONFIG;
