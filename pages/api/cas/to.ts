import { withIronSessionApiRoute } from "iron-session/next";
import { NextApiRequest, NextApiResponse } from "next";
import { casV1Tickets, casV1Users } from "~/lib/cas";
import { sessionOptions } from "~/lib/session";

async function handler(request: NextApiRequest, response: NextApiResponse) {
    const tgt = await casV1Tickets(request.session.user.casId, request.session.user.casPassword)
    const {redirectUrl} = request.query
    const st = await fetch(tgt, {
        method: 'POST',
        body: new URLSearchParams({ service: redirectUrl as string })
    }).then(r => r.text())

    const u = new URL(redirectUrl as string)
    u.searchParams.set('ticket', st)
    response.redirect(302, u.toString())
}

export default withIronSessionApiRoute(handler, sessionOptions);
