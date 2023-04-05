export async function casV1Users(username, password) {
    return  await fetch('https://sso.westlake.edu.cn/cas/v1/users', {
        method: 'POST',
        body: new URLSearchParams({username, password})
    }).then(r => r.json()["authentication"]["successes"]["RestAuthenticationHandler"]["principal"]["attributes"])
}