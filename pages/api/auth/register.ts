import { withIronSessionApiRoute } from "iron-session/next";
import { sessionOptions } from "~/lib/session";
import { NextApiRequest, NextApiResponse } from "next";
// import { register } from "~/lib/webauthn";
import {
    generateRegistrationOptions,
    verifyRegistrationResponse,
} from '@simplewebauthn/server';
import mongo from "~/lib/mongo";
import { ObjectId } from "mongodb";
import { Authenticator, User } from "~/lib/user";

async function handler(request: NextApiRequest, response: NextApiResponse) {
    // Human-readable title for your website
    const rpName = 'SimpleWebAuthn Example';
    // A unique identifier for your website
    const rpID = 'localhost';
    // The URL at which registrations and authentications should occur
    const origin = `http://${rpID}:3000`;
    try {
        let verification = await verifyRegistrationResponse({
            response: request.body,
            expectedChallenge: request.session.challenge,
            expectedOrigin: origin,
            expectedRPID: rpID,
        });
        const { registrationInfo } = verification;
        const { credentialPublicKey, credentialID, counter } = registrationInfo;

        const newAuthenticator: Authenticator = {
            credentialID,
            credentialPublicKey,
            counter,
        };

        const col = (await mongo).db('lakehub').collection('users')
        await col.updateOne(
            { _id: new ObjectId(request.session.userId) },
            { $push: { authenticator: newAuthenticator } }
        )
        return response.json(verification)
    } catch (error) {
        console.error(error);
        return response.status(400).send({ error: error.message });
    }
}

export default withIronSessionApiRoute(handler, sessionOptions);
