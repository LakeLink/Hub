import { withIronSessionApiRoute } from "iron-session/next";
import { NextApiRequest, NextApiResponse } from "next";
import { casV1Users } from "~/lib/cas";
import { sessionOptions } from "~/lib/session";
import mongo from '~/lib/mongo'
import { User } from "~/lib/user";

async function handler(request: NextApiRequest, response: NextApiResponse) {
  try {
    const [userInfo, client] = await Promise.all([await casV1Users(request.body.username, request.body.password), await mongo])
    const db = client.db('lakehub')
    const r = await db.collection<User>('users').findOneAndReplace(
      { casId: request.body.username },
      {
        verified: true,
        role: userInfo.identity,
        realName: userInfo.name,
        org: userInfo.organization,
        casId: request.body.username,
        casPassword: request.body.password,
        authenticators: []
      },
      {
        upsert: true,
        returnDocument: 'after'
      }
    )
    request.session.userId = r.value._id.toString()
    await request.session.save();

    if (r.value.authenticators.length == 0) response.redirect(302, '/auth/register')
    else response.redirect(302, '/')
  } catch (error) {
    response.status(500).json({ message: (error as Error).message });
  }
}

export default withIronSessionApiRoute(handler, sessionOptions);
