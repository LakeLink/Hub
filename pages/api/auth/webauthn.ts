import { withIronSessionApiRoute } from "iron-session/next";
import { sessionOptions } from "~/lib/session";
import { NextApiRequest, NextApiResponse } from "next";
import { ObjectId } from "mongodb";
import { Authenticator, User } from "~/lib/user";
import { auth, genAuthentication, register } from "~/lib/webauthn";
import mongo from "~/lib/mongo";

async function handler(request: NextApiRequest, response: NextApiResponse) {
    switch (request.query['action']) {
        case 'register':
            return response.send(
                register(new ObjectId(request.session.userId), request.body, request.session.challenge)
            )
        case 'genAuthOptions': {
            const col = (await mongo).db('lakehub').collection<User>('users')
            let user = await col.findOne(
                { casId: request.query['casId'] }
            )
            if (!user) return response.status(404)

            let r = genAuthentication(user)
            request.session.challenge = r.challenge
            await request.session.save()
            return response.json(r)
        }
        case 'auth': {
            const col = (await mongo).db('lakehub').collection<User>('users')
            let user = await col.findOne(
                { casId: request.query['casId'] }
            )
            let verification = await auth(user, request.body, request.session.challenge)

            request.session.challenge = null
            if (verification.verified) {
                request.session.userId = user.casId
                col.updateOne(
                    { _id: user._id, authenticators: { credentialID: verification.authenticationInfo.credentialID } },
                    { "authenticators.$.counter": verification.authenticationInfo.newCounter }
                )
            }
            await request.session.save()
            return response.json(verification)
        }
        default:
            return response.status(400)
    }
}

export default withIronSessionApiRoute(handler, sessionOptions);
