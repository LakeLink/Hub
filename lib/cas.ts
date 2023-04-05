export async function casV1Users(username, password) {
    return await fetch('https://sso.westlake.edu.cn/cas/v1/users', {
        method: 'POST',
        body: new URLSearchParams({ username, password })
    }).then(r => r.json()).then(j => j["authentication"]["successes"]["RestAuthenticationHandler"]["principal"]["attributes"])
}

export async function casV1Tickets(username, password) {
    return await fetch('https://sso.westlake.edu.cn/cas/v1/tickets', {
        method: 'POST',
        body: new URLSearchParams({ username, password })
    }).then(r => r.headers.get('location'))
}
