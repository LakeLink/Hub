import { withIronSessionApiRoute } from "iron-session/next";
import { NextApiRequest, NextApiResponse } from "next";
import { casV1Users } from "~/lib/cas";
import { sessionOptions } from "~/lib/session";

async function handler(request: NextApiRequest, response: NextApiResponse) {
  try {
    const userInfo = await casV1Users(request.body.username, request.body.password)
    request.session.user = {
        verified: true,
        role: userInfo.identity,
        realName: userInfo.name,
        organization: userInfo.organization,
        casId: request.body.username,
        casPassword: request.body.password
    }
    await request.session.save();
    response.redirect(302, '/')
  } catch (error) {
    response.status(500).json({ message: (error as Error).message });
  }
}

export default withIronSessionApiRoute(handler, sessionOptions);
