import {
    generateAuthenticationOptions,
    generateRegistrationOptions,
    verifyAuthenticationResponse,
    verifyRegistrationResponse
} from '@simplewebauthn/server'
import { Authenticator, User } from './user';
import mongo from './mongo';
import { Binary, ObjectId } from 'mongodb';
import { AuthenticationResponseJSON, RegistrationResponseJSON } from '@simplewebauthn/typescript-types';

// Human-readable title for your website
const rpName = 'SimpleWebAuthn Example';
// A unique identifier for your website
const rpID = 'localhost';
// The URL at which registrations and authentications should occur
const origin = `http://localhost:3000`;

export async function auth(user: User, response: AuthenticationResponseJSON, expectedChallenge: string) {
    // (Pseudocode} Retrieve an authenticator from the DB that
    // should match the `id` in the returned credential

    const a = user.authenticators.find(e => Buffer.compare(e.credentialID.buffer, Buffer.from(response.rawId, 'base64url')) == 0);
    if (!a) {
        throw new Error(`Could not find authenticator ${response.id} for user ${user.casId}`);
    }

    let verification = await verifyAuthenticationResponse({
            response,
            expectedChallenge,
            expectedOrigin: origin,
            expectedRPID: rpID,
            authenticator: {
                credentialPublicKey: a.credentialPublicKey.buffer,
                credentialID: a.credentialID.buffer,
                counter: a.counter
            }
        });
    return verification
}

export function genAuthentication(user: User) {
    return generateAuthenticationOptions({
        // Require users to use a previously-registered authenticator
        allowCredentials: user.authenticators.map(authenticator => ({
            id: authenticator.credentialID.buffer,
            type: 'public-key',
            // Optional
            //   transports: authenticator.transports,
        })),
        userVerification: 'preferred',
    });
}

export function genRegistration(user: User) {
    return generateRegistrationOptions({
        rpName,
        rpID,
        userID: user.casId,
        userName: user.realName,
        // Don't prompt users for additional information about the authenticator
        // (Recommended for smoother UX)
        attestationType: 'none',
        // Prevent users from re-registering existing authenticators
        // excludeCredentials: user.authenticators.map(c => ({
        //     id: c.credentialID,
        //     type: 'public-key',
        //     // Optional
        //     // transports: c.transports,
        // })),
    });
}

export async function register(userId: ObjectId, response: RegistrationResponseJSON, expectedChallenge: string) {
    let verification = await verifyRegistrationResponse({
        response,
        expectedChallenge,
        expectedRPID: rpID,
        expectedOrigin: origin
    });

    const { registrationInfo } = verification;
    if (!registrationInfo) throw new Error('No registrationInfo')
    const { credentialPublicKey, credentialID, counter } = registrationInfo;

    const newAuthenticator: Authenticator = {
        credentialID: new Binary(credentialID),
        credentialPublicKey: new Binary(credentialPublicKey),
        counter,
    };

    const col = (await mongo).db('lakehub').collection<User>('users')
    let r = await col.updateOne(
        { _id: userId },
        { $addToSet: { authenticators: newAuthenticator } }
    )
    return { success: r.modifiedCount == 1 }
}
