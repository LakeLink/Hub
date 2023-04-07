import { withIronSessionApiRoute } from "iron-session/next";
import { sessionOptions } from "~/lib/session";
import { NextApiRequest, NextApiResponse } from "next";
import { ObjectId } from "mongodb";
import { Authenticator, User } from "~/lib/user";
import { auth, genAuthentication, register } from "~/lib/webAuthn";
import mongo from "~/lib/mongo";

async function handler(request: NextApiRequest, response: NextApiResponse) {
    switch (request.query.action) {
        case 'register':
            return response.json(
                await register(new ObjectId(request.session.userId), request.body, request.session.challenge)
            )
        case 'genAuthOptions': {
            const col = (await mongo).db('lakehub').collection<User>('users')

            let user = await col.findOne(
                { casId: request.body.casId }
            )
            if (!user) return response.status(404).send({ error: 'User not found.' })
            if (user.authenticators.length == 0) return response.status(404).send({ error: 'No authenticator found.' })
            let r = genAuthentication(user)
            request.session.challenge = r.challenge
            await request.session.save()
            return response.json(r)
        }
        case 'auth': {
            const col = (await mongo).db('lakehub').collection<User>('users')
            let user = await col.findOne(
                { casId: request.body.casId }
            )
            let verification = await auth(user, request.body.authResponse, request.session.challenge)

            request.session.challenge = null
            if (verification.verified) {
                request.session.userId = user._id.toString()
                col.updateOne(
                    { _id: user._id, authenticators: { credentialID: verification.authenticationInfo.credentialID } },
                    { $set: { "authenticators.$.counter": verification.authenticationInfo.newCounter } }
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
