const PROXY_CONFIG = [
  {
    context: ["/api"],
    proxyTimeout: 10000,
    target: 'https://localhost:7005',
    secure: false,
    headers: {
      Connection: 'Keep-Alive'
    }
  }
]

module.exports = PROXY_CONFIG;
